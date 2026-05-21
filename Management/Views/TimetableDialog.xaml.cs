using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace FacePass.Management.Views
{
    public partial class TimetableDialog : Window
    {
        private readonly string _baseUrl = "https://mfcyozrkizrbrtpfihdj.supabase.co";
        private readonly string _anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1mY3lvenJraXpyYnJ0cGZpaGRqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwMjcwNDMsImV4cCI6MjA5MjYwMzA0M30.HHuB-oJs4TYEWMZi-7Loe3-cJHjLH8nvnGkBBaliJIE";
        private JObject _existingSlot;
        private string _classId;

        public TimetableDialog(JObject existingSlot = null)
        {
            InitializeComponent();
            _existingSlot = existingSlot;
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);

                // Fetch Courses
                var courseResp = await client.GetAsync($"{_baseUrl}/rest/v1/courses?select=*&order=name.asc");
                courseResp.EnsureSuccessStatusCode();
                var courses = JArray.Parse(await courseResp.Content.ReadAsStringAsync());
                CourseCombo.ItemsSource = courses;

                // Fetch Class ID for BSSE-A
                var classResp = await client.GetAsync($"{_baseUrl}/rest/v1/classes?name=eq.BSSE-A&select=id");
                classResp.EnsureSuccessStatusCode();
                var classes = JArray.Parse(await classResp.Content.ReadAsStringAsync());
                if (classes.Count > 0)
                {
                    _classId = classes[0]["id"].ToString();
                }

                if (_existingSlot != null)
                {
                    DayCombo.Text = _existingSlot["day_of_week"]?.ToString();
                    StartTimeBox.Text = _existingSlot["start_time"]?.ToString();
                    EndTimeBox.Text = _existingSlot["end_time"]?.ToString();
                    
                    var courseId = _existingSlot["course_id"]?.ToString();
                    foreach (JObject item in CourseCombo.Items)
                    {
                        if (item["id"]?.ToString() == courseId)
                        {
                            CourseCombo.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DayCombo.Text) || CourseCombo.SelectedItem == null ||
                string.IsNullOrWhiteSpace(StartTimeBox.Text) || string.IsNullOrWhiteSpace(EndTimeBox.Text))
            {
                MessageBox.Show("Please fill all fields.");
                return;
            }

            try
            {
                var course = (JObject)CourseCombo.SelectedItem;
                var data = new
                {
                    class_id = _classId,
                    course_id = course["id"].ToString(),
                    day_of_week = DayCombo.Text,
                    start_time = StartTimeBox.Text,
                    end_time = EndTimeBox.Text
                };

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                HttpResponseMessage resp;
                if (_existingSlot == null)
                {
                    resp = await client.PostAsync($"{_baseUrl}/rest/v1/timetable", content);
                }
                else
                {
                    var id = _existingSlot["id"].ToString();
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_baseUrl}/rest/v1/timetable?id=eq.{id}")
                    {
                        Content = content
                    };
                    resp = await client.SendAsync(request);
                }

                resp.EnsureSuccessStatusCode();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save Error: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

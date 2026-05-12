using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace FacePass.Management.Views
{
    public partial class TeacherAssignmentDialog : Window
    {
        private readonly string _baseUrl = "https://mfcyozrkizrbrtpfihdj.supabase.co";
        private readonly string _anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1mY3lvenJraXpyYnJ0cGZpaGRqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwMjcwNDMsImV4cCI6MjA5MjYwMzA0M30.HHuB-oJs4TYEWMZi-7Loe3-cJHjLH8nvnGkBBaliJIE";
        private readonly Guid _teacherId;
        private readonly string _teacherName;
        private readonly string _teacherEmail;

        public TeacherAssignmentDialog(Guid teacherId, string name, string email)
        {
            InitializeComponent();
            _teacherId = teacherId;
            _teacherName = name;
            _teacherEmail = email;
            TeacherInfoText.Text = $"{_teacherName} ({_teacherEmail})";
            LoadClasses();
        }

        private async void LoadClasses()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);
                var resp = await client.GetAsync($"{_baseUrl}/rest/v1/classes?select=*&order=name.asc");
                resp.EnsureSuccessStatusCode();
                var classes = JArray.Parse(await resp.Content.ReadAsStringAsync());
                ClassCombo.ItemsSource = classes;
                if (classes.Count > 0) ClassCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading classes: {ex.Message}");
            }
        }

        private async void ClassCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClassCombo.SelectedItem is JObject selectedClass)
            {
                string classId = selectedClass["id"].ToString();
                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("apikey", _anonKey);
                    var resp = await client.GetAsync($"{_baseUrl}/rest/v1/courses?class_id=eq.{classId}&select=*&order=name.asc");
                    resp.EnsureSuccessStatusCode();
                    var courses = JArray.Parse(await resp.Content.ReadAsStringAsync());
                    CourseCombo.ItemsSource = courses;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading courses: {ex.Message}");
                }
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (CourseCombo.SelectedItem is JObject selectedCourse)
            {
                string courseId = selectedCourse["id"].ToString();
                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("apikey", _anonKey);
                    var payload = new JObject { ["teacher_id"] = _teacherId };
                    var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_baseUrl}/rest/v1/courses?id=eq.{courseId}")
                    {
                        Content = content
                    };
                    var resp = await client.SendAsync(request);
                    resp.EnsureSuccessStatusCode();

                    MessageBox.Show("Teacher assigned successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error assigning teacher: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please select a subject.");
            }
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

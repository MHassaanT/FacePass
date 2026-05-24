using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FacePass.Management.Services;

namespace FacePass.Management.Views
{
    public partial class TimetableDialog : Window
    {
        private JObject _existingSlot;

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
                using var client = SupabaseRestClient.Create();

                var courseResp = await client.GetAsync($"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES?select=*&order=course_name.asc");
                courseResp.EnsureSuccessStatusCode();
                var courses = JArray.Parse(await courseResp.Content.ReadAsStringAsync());
                foreach (JObject course in courses)
                    course["name"] = course["course_name"];
                CourseCombo.ItemsSource = courses;

                if (_existingSlot != null)
                {
                    DayCombo.Text = _existingSlot["day_of_week"]?.ToString();
                    StartTimeBox.Text = _existingSlot["start_time"]?.ToString();
                    EndTimeBox.Text = _existingSlot["end_time"]?.ToString();
                    
                    var courseId = _existingSlot["course_id"]?.ToString();
                    foreach (JObject item in CourseCombo.Items)
                    {
                        if (item["course_id"]?.ToString() == courseId)
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
                    course_id = course["course_id"].ToString(),
                    day_of_week = DayCombo.Text,
                    start_time = StartTimeBox.Text,
                    end_time = EndTimeBox.Text
                };

                using var client = SupabaseRestClient.Create();
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                HttpResponseMessage resp;
                if (_existingSlot == null)
                {
                    var postReq = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseRestClient.BaseUrl}/rest/v1/timetable")
                    {
                        Content = content
                    };
                    postReq.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(postReq);
                }
                else
                {
                    var id = _existingSlot["id"].ToString();
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{SupabaseRestClient.BaseUrl}/rest/v1/timetable?id=eq.{id}")
                    {
                        Content = content
                    };
                    request.Headers.Add("Prefer", "return=representation");
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

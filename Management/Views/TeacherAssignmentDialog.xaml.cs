using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using FacePass.Management.Services;

namespace FacePass.Management.Views
{
    public partial class TeacherAssignmentDialog : Window
    {
        private readonly long _teacherId;
        private readonly string _teacherName;
        private readonly string _teacherEmail;

        public TeacherAssignmentDialog(long teacherId, string name, string email)
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
                using var client = SupabaseRestClient.Create();
                var resp = await client.GetAsync($"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES?select=*&order=course_name.asc");
                resp.EnsureSuccessStatusCode();
                var courses = JArray.Parse(await resp.Content.ReadAsStringAsync());
                foreach (JObject course in courses)
                    course["name"] = course["course_name"];
                ClassCombo.ItemsSource = courses;
                if (courses.Count > 0)
                {
                    ClassCombo.SelectedIndex = 0;
                    CourseCombo.ItemsSource = courses;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading courses: {ex.Message}");
            }
        }

        private void ClassCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClassCombo.SelectedItem is JObject selectedClass)
            {
                CourseCombo.ItemsSource = new[] { selectedClass };
                CourseCombo.SelectedIndex = 0;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (CourseCombo.SelectedItem is JObject selectedCourse)
            {
                string courseId = selectedCourse["course_id"]!.ToString();
                try
                {
                    using var client = SupabaseRestClient.Create();
                    var payload = new JObject { ["teacher_id"] = _teacherId };
                    var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES?course_id=eq.{courseId}")
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

using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using Newtonsoft.Json.Linq;
using FacePass.Management.Services;

namespace FacePass.Management.Views
{
    public partial class StudentAssignmentDialog : Window
    {
        private readonly long _userId;
        private readonly string _userName;
        private readonly long _adminId;

        public StudentAssignmentDialog(long userId, string userName, long adminId)
        {
            InitializeComponent();
            _userId = userId;
            _userName = userName;
            _adminId = adminId;
            StudentNameText.Text = _userName;
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
                if (courses.Count > 0) ClassCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading courses: {ex.Message}");
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ClassCombo.SelectedItem is JObject selectedCourse)
            {
                string courseId = selectedCourse["course_id"]!.ToString();
                string courseName = selectedCourse["name"]!.ToString();

                try
                {
                    using var client = SupabaseRestClient.Create();

                    var studentResp = await client.GetAsync($"{SupabaseRestClient.BaseUrl}/rest/v1/STUDENTS?student_id=eq.{_userId}&select=student_id");
                    studentResp.EnsureSuccessStatusCode();
                    var students = JArray.Parse(await studentResp.Content.ReadAsStringAsync());
                    if (students.Count == 0)
                    {
                        MessageBox.Show("Student record not found. Make sure the user is a student.");
                        return;
                    }
                    string studentId = students[0]["student_id"]!.ToString();

                    var enrollPayload = new JObject { ["student_id"] = studentId, ["course_id"] = courseId };
                    var enrollContent = new StringContent(enrollPayload.ToString(), Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSE_ENROLLMENTS")
                    {
                        Content = enrollContent
                    };
                    request.Headers.Add("Prefer", "resolution=ignore-duplicates");

                    var enrollResp = await client.SendAsync(request);
                    enrollResp.EnsureSuccessStatusCode();

                    var logPayload = new JObject
                    {
                        ["actor_id"] = _adminId,
                        ["action"] = "ASSIGN_STUDENT_COURSE",
                        ["metadata"] = $"Enrolled student {_userName} in course {courseName}."
                    };
                    await client.PostAsync($"{SupabaseRestClient.BaseUrl}/rest/v1/audit_logs", new StringContent(logPayload.ToString(), Encoding.UTF8, "application/json"));

                    MessageBox.Show($"Student enrolled in {courseName}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error in assignment: {ex.Message}");
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

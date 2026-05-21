using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace FacePass.Management.Views
{
    public partial class StudentAssignmentDialog : Window
    {
        private readonly string _baseUrl = "https://mfcyozrkizrbrtpfihdj.supabase.co";
        private readonly string _anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1mY3lvenJraXpyYnJ0cGZpaGRqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwMjcwNDMsImV4cCI6MjA5MjYwMzA0M30.HHuB-oJs4TYEWMZi-7Loe3-cJHjLH8nvnGkBBaliJIE";
        private readonly Guid _userId;
        private readonly string _userName;
        private readonly Guid _adminId;

        public StudentAssignmentDialog(Guid userId, string userName, Guid adminId)
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

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ClassCombo.SelectedItem is JObject selectedClass)
            {
                string classId = selectedClass["id"].ToString();
                string className = selectedClass["name"].ToString();

                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("apikey", _anonKey);

                    // 1. Get Student ID
                    var studentResp = await client.GetAsync($"{_baseUrl}/rest/v1/students?user_id=eq.{_userId}&select=id");
                    studentResp.EnsureSuccessStatusCode();
                    var students = JArray.Parse(await studentResp.Content.ReadAsStringAsync());
                    if (students.Count == 0)
                    {
                        MessageBox.Show("Student record not found. Make sure the user is a student.");
                        return;
                    }
                    string studentId = students[0]["id"].ToString();

                    // 2. PATCH Student Class
                    var patchPayload = new JObject { ["class_id"] = classId };
                    var patchContent = new StringContent(patchPayload.ToString(), Encoding.UTF8, "application/json");
                    var patchReq = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_baseUrl}/rest/v1/students?id=eq.{studentId}") { Content = patchContent };
                    var patchResp = await client.SendAsync(patchReq);
                    patchResp.EnsureSuccessStatusCode();

                    // 3. Fetch Courses for Class
                    var courseResp = await client.GetAsync($"{_baseUrl}/rest/v1/courses?class_id=eq.{classId}&select=id");
                    courseResp.EnsureSuccessStatusCode();
                    var courses = JArray.Parse(await courseResp.Content.ReadAsStringAsync());

                    // 4. Enroll in Courses
                    int enrolledCount = 0;
                    foreach (var course in courses)
                    {
                        string courseId = course["id"].ToString();
                        var enrollPayload = new JObject { ["student_id"] = studentId, ["course_id"] = courseId };
                        var enrollContent = new StringContent(enrollPayload.ToString(), Encoding.UTF8, "application/json");
                        
                        // Use Upsert logic (headers for PostgREST)
                        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/rest/v1/student_enrollments")
                        {
                            Content = enrollContent
                        };
                        request.Headers.Add("Prefer", "resolution=ignore-duplicates");
                        
                        var enrollResp = await client.SendAsync(request);
                        if (enrollResp.IsSuccessStatusCode) enrolledCount++;
                    }

                    // 5. Audit Log
                    var logPayload = new JObject
                    {
                        ["actor_id"] = _adminId,
                        ["action"] = "ASSIGN_STUDENT_CLASS",
                        ["metadata"] = $"Assigned student {_userName} to class {className} and enrolled in {enrolledCount} subjects."
                    };
                    await client.PostAsync($"{_baseUrl}/rest/v1/audit_logs", new StringContent(logPayload.ToString(), Encoding.UTF8, "application/json"));

                    MessageBox.Show($"Student assigned to {className} and enrolled in {enrolledCount} subjects.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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

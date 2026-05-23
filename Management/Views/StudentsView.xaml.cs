using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;

namespace FacePass.Management.Views
{
    public partial class StudentsView : UserControl
    {
        private readonly string _baseUrl = "https://mfcyozrkizrbrtpfihdj.supabase.co";
        private readonly string _anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1mY3lvenJraXpyYnJ0cGZpaGRqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwMjcwNDMsImV4cCI6MjA5MjYwMzA0M30.HHuB-oJs4TYEWMZi-7Loe3-cJHjLH8nvnGkBBaliJIE";
        private readonly Guid _teacherId;

        // Expose selected student information to parent view
        public event Action<Guid, bool> StudentSelected;

        public StudentsView(Guid teacherId)
        {
            InitializeComponent();
            _teacherId = teacherId;
            LoadStudentsAsync();
        }

        private async void LoadStudentsAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);

                // Step 1: Get class_id for the teacher's courses (assuming single class per teacher for simplicity)
                var classUrl = $"{_baseUrl}/rest/v1/courses?select=class_id,teacher_id&teacher_id=eq.{_teacherId}";
                var classResp = await client.GetAsync(classUrl);
                classResp.EnsureSuccessStatusCode();
                var classJson = await classResp.Content.ReadAsStringAsync();
                var classArray = JArray.Parse(classJson);
                if (classArray.Count == 0)
                {
                    MessageBox.Show("No class assigned to this teacher.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    StudentsGrid.ItemsSource = null;
                    return;
                }
                var classId = classArray[0]["class_id"]?.ToString();
                if (string.IsNullOrEmpty(classId))
                {
                    MessageBox.Show("Unable to determine class ID.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Step 2: Get students for that class, joining with users to fetch name and email
                var studentsUrl = $"{_baseUrl}/rest/v1/students?select=id,user_id,face_encoding,created_at,users(name,email)&class_id=eq.{classId}";
                var studentsResp = await client.GetAsync(studentsUrl);
                studentsResp.EnsureSuccessStatusCode();
                var studentsJson = await studentsResp.Content.ReadAsStringAsync();
                var studentsArray = JArray.Parse(studentsJson);

                var studentList = new List<object>();
                foreach (JObject stu in studentsArray)
                {
                    var user = stu["users"] as JObject;
                    var name = user?["name"]?.ToString() ?? "Unknown";
                    var email = user?["email"]?.ToString() ?? "Unknown";
                    var createdAt = stu["created_at"]?.ToObject<DateTime?>();
                    var dateCreated = createdAt?.ToString("dd MMM yyyy") ?? "";
                    var faceEnc = stu["face_encoding"]?.ToString();
                    bool enrolled = !string.IsNullOrEmpty(faceEnc);
                    var guid = Guid.Parse(stu["id"].ToString());
                    studentList.Add(new
                    {
                        Id = guid,
                        Name = name,
                        Email = email,
                        DateCreated = dateCreated,
                        BiometricEnrolled = enrolled
                    });
                }

                StudentsGrid.ItemsSource = studentList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading students: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStudentsAsync();
        }

        private void StudentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StudentsGrid.SelectedItem == null) return;
            var selected = StudentsGrid.SelectedItem;
            // Using dynamic to extract properties
            dynamic dyn = selected;
            Guid id = dyn.Id;
            bool enrolled = dyn.BiometricEnrolled;
            StudentSelected?.Invoke(id, enrolled);
        }
    }
}

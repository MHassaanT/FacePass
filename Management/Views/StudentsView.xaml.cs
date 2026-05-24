using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using FacePass.Management.Services;

namespace FacePass.Management.Views
{
    public partial class StudentsView : UserControl
    {
        private readonly long _teacherId;

        public event Action<long, bool> StudentSelected;

        public StudentsView(long teacherId)
        {
            InitializeComponent();
            _teacherId = teacherId;
            LoadStudentsAsync();
        }

        private async void LoadStudentsAsync()
        {
            try
            {
                using var client = SupabaseRestClient.Create();

                var courseUrl = $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES?teacher_id=eq.{_teacherId}&select=course_id";
                var courseResp = await client.GetAsync(courseUrl);
                courseResp.EnsureSuccessStatusCode();
                var courseArray = JArray.Parse(await courseResp.Content.ReadAsStringAsync());

                if (courseArray.Count == 0)
                {
                    MessageBox.Show("No courses assigned to this teacher.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    StudentsGrid.ItemsSource = null;
                    return;
                }

                var courseIds = courseArray
                    .Select(c => c["course_id"]!.ToString())
                    .Where(id => !string.IsNullOrEmpty(id));
                var courseIdList = string.Join(",", courseIds);

                var enrollUrl =
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSE_ENROLLMENTS?course_id=in.({courseIdList})" +
                    "&select=student_id,STUDENTS(status,enrollment_date,USER(first_name,last_name,email,created_at))";
                var enrollResp = await client.GetAsync(enrollUrl);
                enrollResp.EnsureSuccessStatusCode();
                var enrollArray = JArray.Parse(await enrollResp.Content.ReadAsStringAsync());

                if (enrollArray.Count == 0)
                {
                    StudentsGrid.ItemsSource = null;
                    return;
                }

                var studentIds = enrollArray
                    .Select(e => e["student_id"]!.ToString())
                    .Distinct()
                    .ToList();

                var faceEnrolledIds = new HashSet<string>();
                if (studentIds.Count > 0)
                {
                    var faceUrl =
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/FACE_ENCODINGS?student_id=in.({string.Join(",", studentIds)})&select=student_id";
                    var faceResp = await client.GetAsync(faceUrl);
                    faceResp.EnsureSuccessStatusCode();
                    var faceArray = JArray.Parse(await faceResp.Content.ReadAsStringAsync());
                    foreach (var row in faceArray)
                        faceEnrolledIds.Add(row["student_id"]!.ToString());
                }

                var seenStudents = new HashSet<string>();
                var studentList = new List<object>();

                foreach (JObject enrollment in enrollArray)
                {
                    var studentIdStr = enrollment["student_id"]!.ToString();
                    if (!seenStudents.Add(studentIdStr)) continue;

                    var name = JsonEmbedHelper.FullName(enrollment, "STUDENTS", "USER");
                    var email = JsonEmbedHelper.GetNestedField(enrollment, "STUDENTS", "USER", "email");
                    if (string.IsNullOrEmpty(email)) email = "Unknown";

                    var createdAtStr = JsonEmbedHelper.GetNestedField(enrollment, "STUDENTS", "USER", "created_at");
                    DateTime? createdAt = DateTime.TryParse(createdAtStr, out var dt) ? dt : null;
                    var dateCreated = createdAt?.ToString("dd MMM yyyy") ?? "";
                    bool enrolled = faceEnrolledIds.Contains(studentIdStr);
                    var sid = long.Parse(studentIdStr);

                    studentList.Add(new
                    {
                        Id = sid,
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
            dynamic dyn = StudentsGrid.SelectedItem;
            long id = dyn.Id;
            bool enrolled = dyn.BiometricEnrolled;
            StudentSelected?.Invoke(id, enrolled);
        }
    }
}

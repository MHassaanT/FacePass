using System.Net.Http;
using System.Text;
using System.Windows;
using FacePass.Management.Services;
using Newtonsoft.Json.Linq;

namespace FacePass.Management.Views
{
    public partial class CourseDialog : Window
    {
        private readonly JObject? _existingCourse;
        private JArray _departments = new();
        private JArray _teachers = new();

        public CourseDialog(JObject? existingCourse = null)
        {
            InitializeComponent();
            _existingCourse = existingCourse;

            if (_existingCourse != null)
            {
                Title = "Edit Course";
                CourseNameBox.Text = _existingCourse["course_name"]?.ToString();
                CourseCodeBox.Text = _existingCourse["course_code"]?.ToString();
            }

            _ = LoadLookupsAsync();
        }

        private async Task LoadLookupsAsync()
        {
            try
            {
                using var client = SupabaseRestClient.Create();

                var deptResp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/DEPARTMENT?select=*&order=department_name.asc");
                deptResp.EnsureSuccessStatusCode();

                _departments = JArray.Parse(await deptResp.Content.ReadAsStringAsync());
                foreach (JObject dept in _departments)
                    dept["name"] = dept["department_name"];
                DepartmentCombo.ItemsSource = _departments;

                var teacherResp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/TEACHERS?select=teacher_id,USER(first_name,last_name)&order=teacher_id.asc");
                teacherResp.EnsureSuccessStatusCode();

                _teachers = JArray.Parse(await teacherResp.Content.ReadAsStringAsync());
                foreach (JObject teacher in _teachers)
                {
                    var name = JsonEmbedHelper.FullName(teacher, "USER");
                    teacher["name"] = string.IsNullOrWhiteSpace(name) || name == "Unknown"
                        ? $"Teacher #{teacher["teacher_id"]}"
                        : name;
                }
                TeacherCombo.ItemsSource = _teachers;

                if (_existingCourse != null)
                {
                    if (long.TryParse(_existingCourse["department_id"]?.ToString(), out var departmentId))
                    {
                        foreach (JObject dept in _departments)
                        {
                            if (long.TryParse(dept["department_id"]?.ToString(), out var currentId) &&
                                currentId == departmentId)
                            {
                                DepartmentCombo.SelectedItem = dept;
                                break;
                            }
                        }
                    }

                    if (long.TryParse(_existingCourse["teacher_id"]?.ToString(), out var teacherId))
                    {
                        foreach (JObject teacher in _teachers)
                        {
                            if (long.TryParse(teacher["teacher_id"]?.ToString(), out var currentId) &&
                                currentId == teacherId)
                            {
                                TeacherCombo.SelectedItem = teacher;
                                break;
                            }
                        }
                    }
                }

                if (DepartmentCombo.SelectedItem == null && DepartmentCombo.Items.Count > 0)
                    DepartmentCombo.SelectedIndex = 0;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading lookups: {ex.Message}");
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CourseNameBox.Text))
            {
                MessageBox.Show("Please enter a course name.");
                return;
            }

            if (DepartmentCombo.SelectedItem is not JObject selectedDepartment ||
                !long.TryParse(selectedDepartment["department_id"]?.ToString(), out var departmentId))
            {
                MessageBox.Show("Please select a department.");
                return;
            }

            long? teacherId = null;
            if (TeacherCombo.SelectedItem is JObject selectedTeacher &&
                long.TryParse(selectedTeacher["teacher_id"]?.ToString(), out var parsedTeacherId))
            {
                teacherId = parsedTeacherId;
            }

            long? courseCode = null;
            if (!string.IsNullOrWhiteSpace(CourseCodeBox.Text))
            {
                if (!long.TryParse(CourseCodeBox.Text, out var parsedCourseCode))
                {
                    MessageBox.Show("Please enter a valid numeric course code or leave it blank.");
                    return;
                }
                courseCode = parsedCourseCode;
            }

            try
            {
                using var client = SupabaseRestClient.Create();
                var payload = new JObject
                {
                    ["course_name"] = CourseNameBox.Text.Trim(),
                    ["course_code"] = courseCode is null ? JValue.CreateNull() : courseCode.Value,
                    ["department_id"] = departmentId,
                    ["teacher_id"] = teacherId is null ? JValue.CreateNull() : teacherId.Value
                };

                HttpResponseMessage resp;
                if (_existingCourse == null)
                {
                    var request = new HttpRequestMessage(HttpMethod.Post,
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES")
                    {
                        Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(request);
                }
                else
                {
                    string id = _existingCourse["course_id"]?.ToString() ?? "";
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES?course_id=eq.{id}")
                    {
                        Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(request);
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"{(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");
                }

                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
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

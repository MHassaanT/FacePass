using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FacePass.Management.Services;
using Newtonsoft.Json.Linq;

namespace FacePass.Management.Views
{
    public partial class UserDialog : Window
    {
        private readonly long _adminId;
        private readonly JObject _existingUser;
        private JArray _departments = new();
        private JArray _courses = new();
        private bool _loadingLookups;
        private bool _isInitializing = true;

        private static async Task<long> ResolveRoleIdAsync(HttpClient client, string roleName)
        {
            var normalized = roleName.Trim().ToLowerInvariant();
            var url = $"{SupabaseRestClient.BaseUrl}/rest/v1/ROLE?select=role_id,role_name";
            var resp = await client.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            var roles = JArray.Parse(await resp.Content.ReadAsStringAsync());
            foreach (JObject role in roles)
            {
                var name = role["role_name"]?.ToString()?.Trim().ToLowerInvariant();
                if (name == normalized)
                    return long.Parse(role["role_id"]!.ToString());
            }

            throw new InvalidOperationException(
                $"Role \"{roleName}\" was not found in the ROLE table. " +
                "Run Database/seed_lookup_tables.sql in the Supabase SQL Editor.");
        }

        private static async Task EnsureProfileRowAsync(
            HttpClient client,
            long userId,
            string roleName,
            long? departmentId,
            long? courseId)
        {
            var role = roleName.Trim().ToLowerInvariant();
            if (role == "student")
            {
                if (departmentId is null)
                    throw new InvalidOperationException("Please select a department for the student.");

                if (courseId is null)
                    throw new InvalidOperationException("Please select a course for the student.");

                var courseCheck = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES?department_id=eq.{departmentId}&select=course_id");
                courseCheck.EnsureSuccessStatusCode();

                var validCourses = JArray.Parse(await courseCheck.Content.ReadAsStringAsync());
                var chosenCourseId = courseId.Value.ToString();
                var courseMatch = false;
                foreach (JObject course in validCourses)
                {
                    if (course["course_id"]?.ToString() == chosenCourseId)
                    {
                        courseMatch = true;
                        break;
                    }
                }

                if (!courseMatch)
                    throw new InvalidOperationException("The selected course does not belong to the selected department.");

                var studentPayload = new JObject
                {
                    ["student_id"] = userId,
                    ["enrollment_date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    ["status"] = "active"
                };
                var studentRequest = new HttpRequestMessage(HttpMethod.Post,
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/STUDENTS")
                {
                    Content = new StringContent(studentPayload.ToString(), Encoding.UTF8, "application/json")
                };
                studentRequest.Headers.Add("Prefer", "resolution=ignore-duplicates");
                var studentResp = await client.SendAsync(studentRequest);
                if (!studentResp.IsSuccessStatusCode && (int)studentResp.StatusCode != 409)
                    studentResp.EnsureSuccessStatusCode();

                var enrollPayload = new JObject
                {
                    ["student_id"] = userId,
                    ["course_id"] = courseId.Value
                };
                var enrollRequest = new HttpRequestMessage(HttpMethod.Post,
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSE_ENROLLMENTS")
                {
                    Content = new StringContent(enrollPayload.ToString(), Encoding.UTF8, "application/json")
                };
                enrollRequest.Headers.Add("Prefer", "resolution=ignore-duplicates");
                var enrollResp = await client.SendAsync(enrollRequest);
                if (!enrollResp.IsSuccessStatusCode && (int)enrollResp.StatusCode != 409)
                    enrollResp.EnsureSuccessStatusCode();
            }
            else if (role == "teacher")
            {
                if (departmentId is null)
                    throw new InvalidOperationException("Please select a department for the teacher.");

                var payload = new JObject
                {
                    ["teacher_id"] = userId,
                    ["hire_date"] = DateTime.UtcNow.ToString("o"),
                    ["department_id"] = departmentId.Value
                };
                var request = new HttpRequestMessage(HttpMethod.Post,
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/TEACHERS")
                {
                    Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Prefer", "resolution=ignore-duplicates");
                var resp = await client.SendAsync(request);
                if (!resp.IsSuccessStatusCode && (int)resp.StatusCode != 409)
                    resp.EnsureSuccessStatusCode();
            }
        }

        public UserDialog(long adminId, JObject existingUser = null)
        {
            _adminId = adminId;
            _existingUser = existingUser;
            InitializeComponent();

            DepartmentPanel.Visibility = Visibility.Collapsed;
            CoursePanel.Visibility = Visibility.Collapsed;

            if (RoleCombo.Items.Count > 0)
                RoleCombo.SelectedIndex = 0;

            if (_existingUser != null)
            {
                Title = "Edit User";
                NameBox.Text = _existingUser["name"]?.ToString();
                EmailBox.Text = _existingUser["email"]?.ToString();

                string role = _existingUser["role"]?.ToString();
                foreach (ComboBoxItem item in RoleCombo.Items)
                {
                    if (item.Content?.ToString() == role)
                    {
                        RoleCombo.SelectedItem = item;
                        break;
                    }
                }
            }

            _ = LoadDepartmentsAndCourses();
        }

        private static string SelectedRole(ComboBox comboBox)
        {
            return (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim().ToLowerInvariant()
                   ?? "student";
        }

        private long? SelectedDepartmentId()
        {
            if (DepartmentCombo.SelectedItem is not JObject department)
                return null;

            return long.TryParse(department["department_id"]?.ToString(), out var departmentId)
                ? departmentId
                : null;
        }

        private long? SelectedCourseId()
        {
            if (CourseCombo.SelectedItem is not JObject course)
                return null;

            return long.TryParse(course["course_id"]?.ToString(), out var courseId)
                ? courseId
                : null;
        }

        private async Task LoadDepartmentsAndCourses()
        {
            _loadingLookups = true;

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

                var courseResp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES?select=*&order=course_name.asc");
                courseResp.EnsureSuccessStatusCode();

                _courses = JArray.Parse(await courseResp.Content.ReadAsStringAsync());
                foreach (JObject course in _courses)
                    course["name"] = course["course_name"];

                if (DepartmentCombo.Items.Count > 0 && DepartmentCombo.SelectedItem == null)
                    DepartmentCombo.SelectedIndex = 0;

                _isInitializing = false;
                ApplyRoleVisibility();
                FilterCoursesForSelectedDepartment();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading lookup data: {ex.Message}");
            }
            finally
            {
                _loadingLookups = false;
            }
        }

        private void ApplyRoleVisibility()
        {
            if (_isInitializing)
                return;

            var role = SelectedRole(RoleCombo);
            DepartmentPanel.Visibility = role == "admin" ? Visibility.Collapsed : Visibility.Visible;
            CoursePanel.Visibility = role == "student" ? Visibility.Visible : Visibility.Collapsed;

            if (role == "student" && DepartmentCombo.SelectedItem == null && DepartmentCombo.Items.Count > 0)
                DepartmentCombo.SelectedIndex = 0;
        }

        private void FilterCoursesForSelectedDepartment()
        {
            if (_isInitializing)
                return;

            if (SelectedRole(RoleCombo) != "student")
            {
                CourseCombo.ItemsSource = null;
                CourseCombo.SelectedIndex = -1;
                return;
            }

            var departmentId = SelectedDepartmentId();
            if (departmentId is null)
            {
                CourseCombo.ItemsSource = null;
                return;
            }

            var filtered = new JArray();
            foreach (JObject course in _courses)
            {
                if (long.TryParse(course["department_id"]?.ToString(), out var courseDeptId) &&
                    courseDeptId == departmentId.Value)
                {
                    filtered.Add(course);
                }
            }

            CourseCombo.ItemsSource = filtered;
            if (filtered.Count > 0)
                CourseCombo.SelectedIndex = 0;
            else
                CourseCombo.SelectedIndex = -1;
        }

        private void RoleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingLookups || _isInitializing)
                return;

            ApplyRoleVisibility();
            FilterCoursesForSelectedDepartment();
        }

        private void DepartmentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingLookups || _isInitializing)
                return;

            FilterCoursesForSelectedDepartment();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text) || string.IsNullOrWhiteSpace(EmailBox.Text))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            try
            {
                var nameParts = NameBox.Text.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var roleName = SelectedRole(RoleCombo);
                var departmentId = SelectedDepartmentId();
                var courseId = SelectedCourseId();

                if ((roleName == "student" || roleName == "teacher") && departmentId is null)
                {
                    MessageBox.Show("Please select a department.");
                    return;
                }

                if (roleName == "student" && courseId is null)
                {
                    MessageBox.Show("Please select a course.");
                    return;
                }

                using var client = SupabaseRestClient.Create();
                var roleId = await ResolveRoleIdAsync(client, roleName);

                var payload = new JObject
                {
                    ["first_name"] = nameParts.Length > 0 ? nameParts[0] : NameBox.Text.Trim(),
                    ["last_name"] = nameParts.Length > 1 ? nameParts[1] : "",
                    ["email"] = EmailBox.Text,
                    ["role_id"] = roleId
                };

                if (!string.IsNullOrWhiteSpace(PassBox.Password))
                {
                    payload["password_hash"] = BCrypt.Net.BCrypt.HashPassword(PassBox.Password);
                }
                else if (_existingUser == null)
                {
                    MessageBox.Show("Password is required for new users.");
                    return;
                }

                var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

                HttpResponseMessage resp;
                if (_existingUser == null)
                {
                    var request = new HttpRequestMessage(HttpMethod.Post,
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/USER")
                    {
                        Content = content
                    };
                    request.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(request);
                }
                else
                {
                    string id = _existingUser["id"]?.ToString() ?? _existingUser["user_id"]?.ToString();
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/USER?user_id=eq.{id}")
                    {
                        Content = content
                    };
                    request.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(request);
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var errBody = await resp.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"{(int)resp.StatusCode} {resp.ReasonPhrase}: {errBody}");
                }

                var respJson = await resp.Content.ReadAsStringAsync();
                var results = JArray.Parse(respJson);
                var userObj = results[0] as JObject;
                long userId = long.Parse(userObj!["user_id"]!.ToString());

                if (_existingUser == null)
                {
                    await EnsureProfileRowAsync(client, userId, roleName, departmentId, courseId);
                }

                try
                {
                    var logPayload = new JObject
                    {
                        ["actor_id"] = _adminId == 0 ? null : _adminId,
                        ["action"] = _existingUser == null ? "CREATE_USER" : "UPDATE_USER",
                        ["metadata"] = $"{(_existingUser == null ? "Created" : "Updated")} user: {EmailBox.Text} ({roleName})"
                    };
                    await client.PostAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/audit_logs",
                        new StringContent(logPayload.ToString(), Encoding.UTF8, "application/json"));
                }
                catch
                {
                }

                MessageBox.Show("User saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                if (roleName == "teacher" && _existingUser == null)
                {
                    var assignDialog = new TeacherAssignmentDialog(userId, NameBox.Text, EmailBox.Text);
                    assignDialog.Owner = Window.GetWindow(this);
                    assignDialog.ShowDialog();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

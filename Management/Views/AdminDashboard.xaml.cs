using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FacePass.Management.Services;
using Newtonsoft.Json.Linq;

namespace FacePass.Management.Views
{
    public partial class AdminDashboard : UserControl
    {
        private readonly long _currentUserId;

        public AdminDashboard(long currentUserId)
        {
            _currentUserId = currentUserId;
            InitializeComponent();
            RefreshUsers_Click(null!, null!);
            RefreshLogs();
        }

        private static string SelectedHeader(TabControl tabControl)
        {
            return tabControl.SelectedItem is TabItem tabItem
                ? tabItem.Header?.ToString() ?? ""
                : "";
        }

        private static async Task DeleteRowAsync(HttpClient client, string url)
        {
            var resp = await client.DeleteAsync(url);
            if (resp.IsSuccessStatusCode)
                return;

            var body = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserDialog(_currentUserId);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
                RefreshUsers_Click(null!, null!);
        }

        private async void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var client = SupabaseRestClient.Create();

                var resp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/USER?select=*,ROLE(role_name)&order=first_name.asc");
                resp.EnsureSuccessStatusCode();

                var users = JArray.Parse(await resp.Content.ReadAsStringAsync());
                foreach (JObject user in users)
                {
                    var first = user["first_name"]?.ToString() ?? "";
                    var last = user["last_name"]?.ToString() ?? "";
                    user["name"] = $"{first} {last}".Trim();
                    user["role"] = JsonEmbedHelper.RoleNameFromUser(user);
                    user["id"] = user["user_id"]?.ToString() ?? "";
                }

                UsersGrid.ItemsSource = users;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"User Fetch Error: {ex.Message}");
            }
        }

        private async void RefreshLogs()
        {
            try
            {
                using var client = SupabaseRestClient.Create();

                var resp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/audit_logs" +
                    $"?select=id,actor_id,action,metadata,created_at,USER(first_name,last_name)" +
                    $"&order=created_at.desc&limit=100");
                resp.EnsureSuccessStatusCode();

                var logs = JArray.Parse(await resp.Content.ReadAsStringAsync());

                foreach (JObject log in logs)
                {
                    var actorName = JsonEmbedHelper.FullName(log, "USER");
                    log["ActorName"] = string.IsNullOrEmpty(actorName) || actorName == "Unknown"
                        ? "System Admin"
                        : actorName;
                    log["Timestamp"] = log["created_at"]?.ToString() ?? "";
                    log["Action"] = log["action"]?.ToString() ?? "";
                    log["Metadata"] = log["metadata"]?.ToString() ?? "";
                }

                AuditGrid.ItemsSource = logs;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Logs] Error: {ex.Message}");
            }
        }

        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = UsersGrid.SelectedItem != null;
            EditUserBtn.IsEnabled = hasSelection;
            DeleteUserBtn.IsEnabled = hasSelection;

            if (UsersGrid.SelectedItem is JObject selected)
                AssignClassBtn.IsEnabled = selected["role"]?.ToString() == "student";
            else
                AssignClassBtn.IsEnabled = false;
        }

        private void AssignClass_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is JObject selected)
            {
                if (selected["role"]?.ToString() != "student")
                {
                    MessageBox.Show("Please select a student user.", "Invalid Selection",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                long userId = long.Parse(selected["id"]!.ToString());
                string name = selected["name"]?.ToString() ?? "";

                var dialog = new StudentAssignmentDialog(userId, name, _currentUserId);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                    RefreshUsers_Click(null!, null!);
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is JObject selected)
            {
                var dialog = new UserDialog(_currentUserId, selected);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                    RefreshUsers_Click(null!, null!);
            }
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is JObject selected)
            {
                string name = selected["name"]?.ToString() ?? "";
                string id = selected["id"]?.ToString() ?? "";
                string email = selected["email"]?.ToString() ?? "";

                var result = MessageBox.Show(
                    $"Are you sure you want to delete {name}? This action cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                try
                {
                    using var client = SupabaseRestClient.Create();

                    await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSE_ENROLLMENTS?student_id=eq.{id}");
                    await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/FACE_ENCODINGS?student_id=eq.{id}");
                    await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/attendance_logs?student_id=eq.{id}");
                    await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/STUDENTS?student_id=eq.{id}");
                    await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/TEACHERS?teacher_id=eq.{id}");

                    var nullActor = new JObject { ["actor_id"] = null };
                    var patchReq = new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/audit_logs?actor_id=eq.{id}")
                    {
                        Content = new StringContent(nullActor.ToString(), Encoding.UTF8, "application/json")
                    };
                    await client.SendAsync(patchReq);

                    var deleteResp = await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/USER?user_id=eq.{id}");
                    deleteResp.EnsureSuccessStatusCode();

                    var logPayload = new JObject
                    {
                        ["actor_id"] = _currentUserId == 0 ? JValue.CreateNull() : (JToken)_currentUserId,
                        ["action"] = "DELETE_USER",
                        ["metadata"] = $"Deleted user: {name} ({email})"
                    };
                    await client.PostAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/audit_logs",
                        new StringContent(logPayload.ToString(), Encoding.UTF8, "application/json"));

                    RefreshUsers_Click(null!, null!);
                    RefreshLogs();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Delete Error: {ex.Message}");
                }
            }
        }

        private void DepartmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = DepartmentsGrid.SelectedItem != null;
            EditDepartmentBtn.IsEnabled = hasSelection;
            DeleteDepartmentBtn.IsEnabled = hasSelection;
        }

        private void BuildingsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = BuildingsGrid.SelectedItem != null;
            EditBuildingBtn.IsEnabled = hasSelection;
            DeleteBuildingBtn.IsEnabled = hasSelection;
        }

        private void ClassroomsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = ClassroomsGrid.SelectedItem != null;
            EditClassroomBtn.IsEnabled = hasSelection;
            DeleteClassroomBtn.IsEnabled = hasSelection;
        }

        private void CoursesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = CoursesGrid.SelectedItem != null;
            EditCourseBtn.IsEnabled = hasSelection;
            DeleteCourseBtn.IsEnabled = hasSelection;
        }

        private async void RefreshDepartments_Click(object sender, RoutedEventArgs e) => await RefreshDepartments();

        private async Task RefreshDepartments()
        {
            try
            {
                using var client = SupabaseRestClient.Create();
                var resp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/DEPARTMENT?select=*&order=department_name.asc");
                resp.EnsureSuccessStatusCode();

                var departments = JArray.Parse(await resp.Content.ReadAsStringAsync());
                DepartmentsGrid.ItemsSource = departments;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Department Fetch Error: {ex.Message}");
            }
        }

        private async void RefreshBuildings_Click(object sender, RoutedEventArgs e) => await RefreshBuildings();

        private async Task RefreshBuildings()
        {
            try
            {
                using var client = SupabaseRestClient.Create();
                var resp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/BUILDINGS?select=*&order=building_name.asc");
                resp.EnsureSuccessStatusCode();

                var buildings = JArray.Parse(await resp.Content.ReadAsStringAsync());
                foreach (JObject building in buildings)
                    building["location_coordinates"] = building["location_coordinates"]?.ToString() ?? "";

                BuildingsGrid.ItemsSource = buildings;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Building Fetch Error: {ex.Message}");
            }
        }

        private async void RefreshClassrooms_Click(object sender, RoutedEventArgs e) => await RefreshClassrooms();

        private async Task RefreshClassrooms()
        {
            try
            {
                using var client = SupabaseRestClient.Create();
                var resp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/CLASSROOMS?select=*,BUILDINGS(building_name)&order=room_number.asc");
                resp.EnsureSuccessStatusCode();

                var classrooms = JArray.Parse(await resp.Content.ReadAsStringAsync());
                foreach (JObject classroom in classrooms)
                {
                    classroom["building_name"] = JsonEmbedHelper.GetField(classroom, "BUILDINGS", "building_name");
                    if (string.IsNullOrWhiteSpace(classroom["building_name"]?.ToString()))
                        classroom["building_name"] = "Unassigned";
                    classroom["capacity"] = classroom["capacity"]?.ToString() ?? "";
                }

                ClassroomsGrid.ItemsSource = classrooms;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Classroom Fetch Error: {ex.Message}");
            }
        }

        private async void RefreshCourses_Click(object sender, RoutedEventArgs e) => await RefreshCourses();

        private async Task RefreshCourses()
        {
            try
            {
                using var client = SupabaseRestClient.Create();
                var resp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES?select=*,DEPARTMENT(department_name),TEACHERS(USER(first_name,last_name))&order=course_name.asc");
                resp.EnsureSuccessStatusCode();

                var courses = JArray.Parse(await resp.Content.ReadAsStringAsync());
                foreach (JObject course in courses)
                {
                    course["department_name"] = JsonEmbedHelper.GetField(course, "DEPARTMENT", "department_name");
                    if (string.IsNullOrWhiteSpace(course["department_name"]?.ToString()))
                        course["department_name"] = "Unknown";

                    var teacherName = JsonEmbedHelper.FullName(course, "TEACHERS", "USER");
                    course["teacher_name"] = string.IsNullOrWhiteSpace(teacherName) || teacherName == "Unknown"
                        ? "Unassigned"
                        : teacherName;

                    course["course_code"] = course["course_code"]?.ToString() ?? "";
                }

                CoursesGrid.ItemsSource = courses;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Course Fetch Error: {ex.Message}");
            }
        }

        private void AddDepartment_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DepartmentDialog();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
                _ = RefreshDepartments();
        }

        private void EditDepartment_Click(object sender, RoutedEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is JObject selected)
            {
                var dialog = new DepartmentDialog(selected);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                    _ = RefreshDepartments();
            }
        }

        private async void DeleteDepartment_Click(object sender, RoutedEventArgs e)
        {
            if (DepartmentsGrid.SelectedItem is not JObject selected)
                return;

            string id = selected["department_id"]?.ToString() ?? "";
            string name = selected["department_name"]?.ToString() ?? "this department";

            if (MessageBox.Show($"Delete department '{name}'?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using var client = SupabaseRestClient.Create();
                await DeleteRowAsync(client,
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/DEPARTMENT?department_id=eq.{id}");
                await RefreshDepartments();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Delete Error: {ex.Message}");
            }
        }

        private void AddBuilding_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BuildingDialog();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
                _ = RefreshBuildings();
        }

        private void EditBuilding_Click(object sender, RoutedEventArgs e)
        {
            if (BuildingsGrid.SelectedItem is JObject selected)
            {
                var dialog = new BuildingDialog(selected);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                    _ = RefreshBuildings();
            }
        }

        private async void DeleteBuilding_Click(object sender, RoutedEventArgs e)
        {
            if (BuildingsGrid.SelectedItem is not JObject selected)
                return;

            string id = selected["building_id"]?.ToString() ?? "";
            string name = selected["building_name"]?.ToString() ?? "this building";

            if (MessageBox.Show($"Delete building '{name}'?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using var client = SupabaseRestClient.Create();
                await DeleteRowAsync(client,
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/BUILDINGS?building_id=eq.{id}");
                await RefreshBuildings();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Delete Error: {ex.Message}");
            }
        }

        private void AddClassroom_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClassroomDialog();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
                _ = RefreshClassrooms();
        }

        private void EditClassroom_Click(object sender, RoutedEventArgs e)
        {
            if (ClassroomsGrid.SelectedItem is JObject selected)
            {
                var dialog = new ClassroomDialog(selected);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                    _ = RefreshClassrooms();
            }
        }

        private async void DeleteClassroom_Click(object sender, RoutedEventArgs e)
        {
            if (ClassroomsGrid.SelectedItem is not JObject selected)
                return;

            string id = selected["classroom_id"]?.ToString() ?? "";
            string room = selected["room_number"]?.ToString() ?? "this classroom";

            if (MessageBox.Show($"Delete classroom '{room}'?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using var client = SupabaseRestClient.Create();
                await DeleteRowAsync(client,
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/CLASSROOMS?classroom_id=eq.{id}");
                await RefreshClassrooms();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Delete Error: {ex.Message}");
            }
        }

        private void AddCourse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CourseDialog();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
                _ = RefreshCourses();
        }

        private void EditCourse_Click(object sender, RoutedEventArgs e)
        {
            if (CoursesGrid.SelectedItem is JObject selected)
            {
                var dialog = new CourseDialog(selected);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                    _ = RefreshCourses();
            }
        }

        private async void DeleteCourse_Click(object sender, RoutedEventArgs e)
        {
            if (CoursesGrid.SelectedItem is not JObject selected)
                return;

            string id = selected["course_id"]?.ToString() ?? "";
            string name = selected["course_name"]?.ToString() ?? "this course";

            if (MessageBox.Show($"Delete course '{name}'?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using var client = SupabaseRestClient.Create();
                await DeleteRowAsync(client,
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSES?course_id=eq.{id}");
                await RefreshCourses();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Delete Error: {ex.Message}");
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is not TabControl tabControl)
                return;

            switch (SelectedHeader(tabControl))
            {
                case "User Management":
                    RefreshUsers_Click(null!, null!);
                    break;
                case "Departments":
                    _ = RefreshDepartments();
                    break;
                case "Buildings":
                    _ = RefreshBuildings();
                    break;
                case "Classrooms":
                    _ = RefreshClassrooms();
                    break;
                case "Courses":
                    _ = RefreshCourses();
                    break;
                case "Audit Logs":
                    RefreshLogs();
                    break;
                case "Timetable":
                    RefreshTimetable();
                    break;
            }
        }

        private void RefreshTimetable_Click(object sender, RoutedEventArgs e) => RefreshTimetable();

        private async void RefreshTimetable()
        {
            try
            {
                using var client = SupabaseRestClient.Create();

                var resp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/timetable" +
                    $"?select=*,COURSES(course_name)&order=day_of_week.asc,start_time.asc");
                resp.EnsureSuccessStatusCode();

                var entries = JArray.Parse(await resp.Content.ReadAsStringAsync());
                foreach (var entry in entries)
                {
                    entry["course_name"] = JsonEmbedHelper.GetField(entry, "COURSES", "course_name");
                    if (string.IsNullOrEmpty(entry["course_name"]?.ToString()))
                        entry["course_name"] = "Unknown";
                }

                TimetableGrid.ItemsSource = entries;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Timetable Fetch Error: {ex.Message}");
            }
        }

        private void AddSlot_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TimetableDialog();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true) RefreshTimetable();
        }

        private void EditSlot_Click(object sender, RoutedEventArgs e)
        {
            if (TimetableGrid.SelectedItem is JObject selected)
            {
                var dialog = new TimetableDialog(selected);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true) RefreshTimetable();
            }
            else
            {
                MessageBox.Show("Please select a slot to edit.");
            }
        }
    }
}

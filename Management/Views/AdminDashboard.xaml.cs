using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Text;
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
                    var last  = user["last_name"]?.ToString()  ?? "";
                    user["name"] = $"{first} {last}".Trim();
                    user["role"] = JsonEmbedHelper.RoleNameFromUser(user);
                    user["id"]   = user["user_id"]?.ToString() ?? "";
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

                // FIX Bug 2: audit_logs uses actor_id but the column binding on the
                // DataGrid expects "ActorName", "Timestamp", and "Action"/"Metadata".
                // The embed hint "USER!actor_id(...)" requires PostgREST to know the
                // FK name. Use the simpler left-join syntax that works with our anon RLS.
                var resp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/audit_logs" +
                    $"?select=id,actor_id,action,metadata,created_at,USER(first_name,last_name)" +
                    $"&order=created_at.desc&limit=100");
                resp.EnsureSuccessStatusCode();

                var logs = JArray.Parse(await resp.Content.ReadAsStringAsync());

                foreach (JObject log in logs)
                {
                    // Resolve actor name from embedded USER object (may be null for
                    // system actions where actor_id is null).
                    var actorName = JsonEmbedHelper.FullName(log, "USER");
                    log["ActorName"] = string.IsNullOrEmpty(actorName) || actorName == "Unknown"
                        ? "System Admin"
                        : actorName;

                    // FIX Bug 2: The DataGrid columns bind to "Timestamp", "Action",
                    // "Metadata". Map created_at → Timestamp so the column shows data.
                    log["Timestamp"] = log["created_at"]?.ToString() ?? "";
                    // "action" and "metadata" are already lowercase — add capitalised
                    // aliases so the DataGrid column bindings work regardless of case.
                    log["Action"]   = log["action"]?.ToString()   ?? "";
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
            EditUserBtn.IsEnabled   = hasSelection;
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

                long   userId = long.Parse(selected["id"]!.ToString());
                string name   = selected["name"]?.ToString() ?? "";

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
                string name  = selected["name"]?.ToString()  ?? "";
                string id    = selected["id"]?.ToString()    ?? "";
                string email = selected["email"]?.ToString() ?? "";

                var result = MessageBox.Show(
                    $"Are you sure you want to delete {name}? This action cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                try
                {
                    using var client = SupabaseRestClient.Create();

                    // FIX Bug 3: Delete child rows in dependency order BEFORE deleting
                    // the USER row. The 409 Conflict is a FK violation because STUDENTS,
                    // TEACHERS, COURSE_ENROLLMENTS, and audit_logs all reference USER.
                    // Step 1 – remove course enrollments for this user (student path)
                    await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/COURSE_ENROLLMENTS?student_id=eq.{id}");

                    // Step 2 – remove face encodings
                    await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/FACE_ENCODINGS?student_id=eq.{id}");

                    // Step 3 – remove attendance logs
                    await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/attendance_logs?student_id=eq.{id}");

                    // Step 4 – remove the STUDENTS profile row (if it exists)
                    await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/STUDENTS?student_id=eq.{id}");

                    // Step 5 – remove the TEACHERS profile row (if it exists)
                    await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/TEACHERS?teacher_id=eq.{id}");

                    // Step 6 – null-out audit_log actor_ids that reference this user
                    // so the audit history is preserved but the FK is released.
                    var nullActor = new JObject { ["actor_id"] = null };
                    var patchReq = new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/audit_logs?actor_id=eq.{id}")
                    {
                        Content = new StringContent(nullActor.ToString(), Encoding.UTF8, "application/json")
                    };
                    await client.SendAsync(patchReq);

                    // Step 7 – finally delete the USER row
                    var deleteResp = await client.DeleteAsync(
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/USER?user_id=eq.{id}");
                    deleteResp.EnsureSuccessStatusCode();

                    // Audit the deletion
                    var logPayload = new JObject
                    {
                        ["actor_id"] = _currentUserId == 0 ? JValue.CreateNull() : (JToken)_currentUserId,
                        ["action"]   = "DELETE_USER",
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

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                if (tabControl.SelectedIndex == 1)      RefreshLogs();
                else if (tabControl.SelectedIndex == 2) RefreshTimetable();
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
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
            {
                RefreshUsers_Click(null!, null!);
            }
        }

        private async void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                using var client = SupabaseRestClient.Create();
                
                var resp = await client.GetAsync($"{SupabaseRestClient.BaseUrl}/rest/v1/USER?select=*,ROLE(role_name)&order=first_name.asc");
                resp.EnsureSuccessStatusCode();
                
                var json = await resp.Content.ReadAsStringAsync();
                var users = JArray.Parse(json);

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

                var resp = await client.GetAsync($"{SupabaseRestClient.BaseUrl}/rest/v1/audit_logs?select=*,USER!actor_id(first_name,last_name)&order=created_at.desc&limit=100");
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var logs = JArray.Parse(json);
                
                foreach (JObject log in logs)
                {
                    var actorName = JsonEmbedHelper.FullName(log, "USER");
                    log["ActorName"] = actorName == "Unknown" ? "System Admin" : actorName;
                    log["Timestamp"] = log["created_at"];
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
            {
                AssignClassBtn.IsEnabled = selected["role"]?.ToString() == "student";
            }
            else
            {
                AssignClassBtn.IsEnabled = false;
            }
        }

        private void AssignClass_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is JObject selected)
            {
                if (selected["role"]?.ToString() != "student")
                {
                    MessageBox.Show("Please select a student user.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                long userId = long.Parse(selected["id"]!.ToString());
                string name = selected["name"]?.ToString();
                
                var dialog = new StudentAssignmentDialog(userId, name, _currentUserId);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    RefreshUsers_Click(null!, null!);
                }
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is JObject selected)
            {
                var dialog = new UserDialog(_currentUserId, selected);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    RefreshUsers_Click(null!, null!);
                }
            }
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is JObject selected)
            {
                string name = selected["name"]?.ToString();
                string id = selected["id"]?.ToString();

                var result = MessageBox.Show($"Are you sure you want to delete {name}? This action cannot be undone.", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var client = SupabaseRestClient.Create();
                        var resp = await client.DeleteAsync($"{SupabaseRestClient.BaseUrl}/rest/v1/USER?user_id=eq.{id}");
                        resp.EnsureSuccessStatusCode();

                        var logPayload = new JObject
                        {
                            ["actor_id"] = _currentUserId,
                            ["action"] = "DELETE_USER",
                            ["metadata"] = $"Deleted user: {name} ({selected["email"]})"
                        };
                        await client.PostAsync($"{SupabaseRestClient.BaseUrl}/rest/v1/audit_logs", new StringContent(logPayload.ToString(), Encoding.UTF8, "application/json"));

                        RefreshUsers_Click(null!, null!);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Delete Error: {ex.Message}");
                    }
                }
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                if (tabControl.SelectedIndex == 1) RefreshLogs();
                else if (tabControl.SelectedIndex == 2) RefreshTimetable();
            }
        }

        private void RefreshTimetable_Click(object sender, RoutedEventArgs e) => RefreshTimetable();

        private async void RefreshTimetable()
        {
            try
            {
                using var client = SupabaseRestClient.Create();

                var resp = await client.GetAsync($"{SupabaseRestClient.BaseUrl}/rest/v1/timetable?select=*,COURSES(course_name)&order=day_of_week.asc,start_time.asc");
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var entries = JArray.Parse(json);

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
            if (dialog.ShowDialog() == true)
            {
                RefreshTimetable();
            }
        }

        private void EditSlot_Click(object sender, RoutedEventArgs e)
        {
            if (TimetableGrid.SelectedItem is JObject selected)
            {
                var dialog = new TimetableDialog(selected);
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    RefreshTimetable();
                }
            }
            else
            {
                MessageBox.Show("Please select a slot to edit.");
            }
        }
    }
}

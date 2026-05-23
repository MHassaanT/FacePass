using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Text;

namespace FacePass.Management.Views
{
    public partial class AdminDashboard : UserControl
    {
        private readonly string _baseUrl = "https://mfcyozrkizrbrtpfihdj.supabase.co";
        private readonly string _anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1mY3lvenJraXpyYnJ0cGZpaGRqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwMjcwNDMsImV4cCI6MjA5MjYwMzA0M30.HHuB-oJs4TYEWMZi-7Loe3-cJHjLH8nvnGkBBaliJIE";
        private readonly Guid _currentUserId;

        public AdminDashboard(Guid currentUserId)
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
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);
                
                var resp = await client.GetAsync($"{_baseUrl}/rest/v1/users?select=*&order=name.asc");
                resp.EnsureSuccessStatusCode();
                
                var json = await resp.Content.ReadAsStringAsync();
                var users = Newtonsoft.Json.Linq.JArray.Parse(json);
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
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);

                var resp = await client.GetAsync($"{_baseUrl}/rest/v1/audit_logs?select=*,actor_id(name)&order=created_at.desc&limit=100");
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var logs = Newtonsoft.Json.Linq.JArray.Parse(json);
                
                // Flatten actor name for easier binding
                foreach (var log in logs)
                {
                    log["ActorName"] = log["actor_id"]?["name"] ?? "System Admin";
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

            // Enable Assign Class only if the selected user is a student
            if (UsersGrid.SelectedItem is Newtonsoft.Json.Linq.JObject selected)
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
            if (UsersGrid.SelectedItem is Newtonsoft.Json.Linq.JObject selected)
            {
                if (selected["role"]?.ToString() != "student")
                {
                    MessageBox.Show("Please select a student user.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Guid userId = Guid.Parse(selected["id"].ToString());
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
            if (UsersGrid.SelectedItem is Newtonsoft.Json.Linq.JObject selected)
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
            if (UsersGrid.SelectedItem is Newtonsoft.Json.Linq.JObject selected)
            {
                string name = selected["name"]?.ToString();
                string id = selected["id"]?.ToString();

                var result = MessageBox.Show($"Are you sure you want to delete {name}? This action cannot be undone.", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("apikey", _anonKey);
                        var resp = await client.DeleteAsync($"{_baseUrl}/rest/v1/users?id=eq.{id}");
                        resp.EnsureSuccessStatusCode();

                        // Audit Log
                        var logPayload = new Newtonsoft.Json.Linq.JObject
                        {
                            ["actor_id"] = _currentUserId,
                            ["action"] = "DELETE_USER",
                            ["metadata"] = $"Deleted user: {name} ({selected["email"]})"
                        };
                        await client.PostAsync($"{_baseUrl}/rest/v1/audit_logs", new StringContent(logPayload.ToString(), Encoding.UTF8, "application/json"));

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
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);

                var resp = await client.GetAsync($"{_baseUrl}/rest/v1/timetable?select=*,courses(name)&order=day_of_week.asc,start_time.asc");
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync();
                var entries = Newtonsoft.Json.Linq.JArray.Parse(json);

                foreach (var entry in entries)
                {
                    entry["course_name"] = entry["courses"]?["name"] ?? "Unknown";
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
            if (TimetableGrid.SelectedItem is Newtonsoft.Json.Linq.JObject selected)
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

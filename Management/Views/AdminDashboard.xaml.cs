using System.Windows;
using System.Windows.Controls;
using System.Net.Http;

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

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl && tabControl.SelectedIndex == 1) // Audit Logs Tab
            {
                RefreshLogs();
            }
        }
    }
}

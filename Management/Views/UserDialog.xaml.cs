using System;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using BCrypt.Net;

namespace FacePass.Management.Views
{
    public partial class UserDialog : Window
    {
        private readonly string _baseUrl = "https://mfcyozrkizrbrtpfihdj.supabase.co";
        private readonly string _anonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1mY3lvenJraXpyYnJ0cGZpaGRqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwMjcwNDMsImV4cCI6MjA5MjYwMzA0M30.HHuB-oJs4TYEWMZi-7Loe3-cJHjLH8nvnGkBBaliJIE";
        private readonly Guid _adminId;

        public UserDialog(Guid adminId)
        {
            _adminId = adminId;
            InitializeComponent();
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
                // 1. Hash Password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(PassBox.Password);

                // 2. Prepare Payload
                var payload = new JObject
                {
                    ["name"] = NameBox.Text,
                    ["email"] = EmailBox.Text,
                    ["password_hash"] = hashedPassword,
                    ["role"] = (RoleCombo.SelectedItem as ComboBoxItem)?.Content.ToString()
                };

                // 3. POST to Supabase
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);
                var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                
                var resp = await client.PostAsync($"{_baseUrl}/rest/v1/users", content);
                resp.EnsureSuccessStatusCode();

                // 4. Audit Log Entry
                try
                {
                    var logPayload = new JObject
                    {
                        ["actor_id"] = _adminId == Guid.Empty ? (Guid?)null : _adminId,
                        ["action"] = "CREATE_USER",
                        ["metadata"] = $"Created user: {EmailBox.Text} ({payload["role"]})"
                    };
                    await client.PostAsync($"{_baseUrl}/rest/v1/audit_logs", new StringContent(logPayload.ToString(), Encoding.UTF8, "application/json"));
                }
                catch { /* Ignore log failures to avoid blocking user creation */ }

                MessageBox.Show("User saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

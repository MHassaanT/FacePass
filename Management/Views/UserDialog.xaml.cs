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
        private readonly JObject _existingUser;

        public UserDialog(Guid adminId, JObject existingUser = null)
        {
            _adminId = adminId;
            _existingUser = existingUser;
            InitializeComponent();

            if (_existingUser != null)
            {
                this.Title = "Edit User";
                NameBox.Text = _existingUser["name"]?.ToString();
                EmailBox.Text = _existingUser["email"]?.ToString();
                
                string role = _existingUser["role"]?.ToString();
                foreach (ComboBoxItem item in RoleCombo.Items)
                {
                    if (item.Content.ToString() == role)
                    {
                        RoleCombo.SelectedItem = item;
                        break;
                    }
                }
            }
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
                // 1. Prepare Payload
                var payload = new JObject
                {
                    ["name"] = NameBox.Text,
                    ["email"] = EmailBox.Text,
                    ["role"] = (RoleCombo.SelectedItem as ComboBoxItem)?.Content.ToString()
                };

                // Hash Password ONLY if provided (required for new, optional for edit)
                if (!string.IsNullOrWhiteSpace(PassBox.Password))
                {
                    payload["password_hash"] = BCrypt.Net.BCrypt.HashPassword(PassBox.Password);
                }
                else if (_existingUser == null)
                {
                    MessageBox.Show("Password is required for new users.");
                    return;
                }

                // 2. Save to Supabase
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", _anonKey);
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");
                var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                
                HttpResponseMessage resp;
                if (_existingUser == null)
                {
                    resp = await client.PostAsync($"{_baseUrl}/rest/v1/users", content);
                }
                else
                {
                    string id = _existingUser["id"].ToString();
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_baseUrl}/rest/v1/users?id=eq.{id}")
                    {
                        Content = content
                    };
                    resp = await client.SendAsync(request);
                }
                resp.EnsureSuccessStatusCode();

                var respJson = await resp.Content.ReadAsStringAsync();
                var results = JArray.Parse(respJson);
                var userObj = results[0] as JObject;
                Guid userId = Guid.Parse(userObj["id"].ToString());

                // 3. Audit Log Entry
                try
                {
                    var logPayload = new JObject
                    {
                        ["actor_id"] = _adminId == Guid.Empty ? (Guid?)null : _adminId,
                        ["action"] = _existingUser == null ? "CREATE_USER" : "UPDATE_USER",
                        ["metadata"] = $"{(_existingUser == null ? "Created" : "Updated")} user: {EmailBox.Text} ({payload["role"]})"
                    };
                    await client.PostAsync($"{_baseUrl}/rest/v1/audit_logs", new StringContent(logPayload.ToString(), Encoding.UTF8, "application/json"));
                }
                catch { }

                MessageBox.Show("User saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // 4. Teacher Assignment Hook (New users only)
                if (payload["role"]?.ToString() == "teacher" && _existingUser == null)
                {
                    var assignDialog = new TeacherAssignmentDialog(userId, NameBox.Text, EmailBox.Text);
                    assignDialog.Owner = Window.GetWindow(this);
                    assignDialog.ShowDialog();
                }

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

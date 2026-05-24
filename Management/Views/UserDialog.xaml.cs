using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using FacePass.Management.Services;

namespace FacePass.Management.Views
{
    public partial class UserDialog : Window
    {
        private readonly long _adminId;
        private readonly JObject _existingUser;

        private static async Task<long> ResolveRoleIdAsync(HttpClient client, string roleName)
        {
            var normalized = roleName.Trim().ToLowerInvariant();
            var url =
                $"{SupabaseRestClient.BaseUrl}/rest/v1/ROLE?select=role_id,role_name";
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

        private static async Task EnsureProfileRowAsync(HttpClient client, long userId, string roleName)
        {
            var role = roleName.Trim().ToLowerInvariant();
            if (role == "student")
            {
                var payload = new JObject
                {
                    ["student_id"] = userId,
                    ["enrollment_date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    ["status"] = "active"
                };
                var req = new HttpRequestMessage(HttpMethod.Post,
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/STUDENTS")
                {
                    Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
                };
                req.Headers.Add("Prefer", "resolution=ignore-duplicates");
                var resp = await client.SendAsync(req);
                if (!resp.IsSuccessStatusCode && (int)resp.StatusCode != 409)
                    resp.EnsureSuccessStatusCode();
            }
            else if (role == "teacher")
            {
                var deptResp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/DEPARTMENT?select=department_id&limit=1");
                deptResp.EnsureSuccessStatusCode();
                var depts = JArray.Parse(await deptResp.Content.ReadAsStringAsync());
                if (depts.Count == 0)
                    return;

                var payload = new JObject
                {
                    ["teacher_id"] = userId,
                    ["hire_date"] = DateTime.UtcNow.ToString("o"),
                    ["department_id"] = depts[0]["department_id"]
                };
                var req = new HttpRequestMessage(HttpMethod.Post,
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/TEACHERS")
                {
                    Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
                };
                req.Headers.Add("Prefer", "resolution=ignore-duplicates");
                var resp = await client.SendAsync(req);
                if (!resp.IsSuccessStatusCode && (int)resp.StatusCode != 409)
                    resp.EnsureSuccessStatusCode();
            }
        }

        public UserDialog(long adminId, JObject existingUser = null)
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
                var nameParts = NameBox.Text.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var roleName = (RoleCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "student";

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
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseRestClient.BaseUrl}/rest/v1/USER")
                    {
                        Content = content
                    };
                    request.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(request);
                }
                else
                {
                    string id = _existingUser["id"]?.ToString() ?? _existingUser["user_id"]?.ToString();
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{SupabaseRestClient.BaseUrl}/rest/v1/USER?user_id=eq.{id}")
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
                long userId = long.Parse(userObj["user_id"]!.ToString());

                if (_existingUser == null)
                {
                    try
                    {
                        await EnsureProfileRowAsync(client, userId, roleName);
                    }
                    catch (Exception profileEx)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[UserDialog] Profile row: {profileEx.Message}");
                    }
                }

                try
                {
                    var logPayload = new JObject
                    {
                        ["actor_id"] = _adminId == 0 ? null : _adminId,
                        ["action"] = _existingUser == null ? "CREATE_USER" : "UPDATE_USER",
                        ["metadata"] = $"{(_existingUser == null ? "Created" : "Updated")} user: {EmailBox.Text} ({roleName})"
                    };
                    await client.PostAsync($"{SupabaseRestClient.BaseUrl}/rest/v1/audit_logs", new StringContent(logPayload.ToString(), Encoding.UTF8, "application/json"));
                }
                catch { }

                MessageBox.Show("User saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                if (roleName == "teacher" && _existingUser == null)
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

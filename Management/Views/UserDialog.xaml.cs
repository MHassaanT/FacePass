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
        private readonly string _baseUrl = "https://YOUR-PROJECT.supabase.co";
        private readonly string _anonKey = "YOUR-ANON-KEY";

        public UserDialog()
        {
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

using System.Net.Http;
using System.Text;
using System.Windows;
using FacePass.Management.Services;
using Newtonsoft.Json.Linq;

namespace FacePass.Management.Views
{
    public partial class DepartmentDialog : Window
    {
        private readonly JObject? _existingDepartment;

        public DepartmentDialog(JObject? existingDepartment = null)
        {
            InitializeComponent();
            _existingDepartment = existingDepartment;

            if (_existingDepartment != null)
            {
                Title = "Edit Department";
                DepartmentNameBox.Text = _existingDepartment["department_name"]?.ToString();
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DepartmentNameBox.Text))
            {
                MessageBox.Show("Please enter a department name.");
                return;
            }

            try
            {
                using var client = SupabaseRestClient.Create();
                var payload = new JObject
                {
                    ["department_name"] = DepartmentNameBox.Text.Trim()
                };

                HttpResponseMessage resp;
                if (_existingDepartment == null)
                {
                    var request = new HttpRequestMessage(HttpMethod.Post,
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/DEPARTMENT")
                    {
                        Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(request);
                }
                else
                {
                    string id = _existingDepartment["department_id"]?.ToString() ?? "";
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/DEPARTMENT?department_id=eq.{id}")
                    {
                        Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(request);
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"{(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");
                }

                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Save Error: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

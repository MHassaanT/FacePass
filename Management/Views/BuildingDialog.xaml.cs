using System.Net.Http;
using System.Text;
using System.Windows;
using FacePass.Management.Services;
using Newtonsoft.Json.Linq;

namespace FacePass.Management.Views
{
    public partial class BuildingDialog : Window
    {
        private readonly JObject? _existingBuilding;

        public BuildingDialog(JObject? existingBuilding = null)
        {
            InitializeComponent();
            _existingBuilding = existingBuilding;

            if (_existingBuilding != null)
            {
                Title = "Edit Building";
                BuildingNameBox.Text = _existingBuilding["building_name"]?.ToString();
                CoordinatesBox.Text = _existingBuilding["location_coordinates"]?.ToString();
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(BuildingNameBox.Text))
            {
                MessageBox.Show("Please enter a building name.");
                return;
            }

            try
            {
                using var client = SupabaseRestClient.Create();
                var payload = new JObject
                {
                    ["building_name"] = BuildingNameBox.Text.Trim(),
                    ["location_coordinates"] = string.IsNullOrWhiteSpace(CoordinatesBox.Text)
                        ? JValue.CreateNull()
                        : CoordinatesBox.Text.Trim()
                };

                HttpResponseMessage resp;
                if (_existingBuilding == null)
                {
                    var request = new HttpRequestMessage(HttpMethod.Post,
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/BUILDINGS")
                    {
                        Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(request);
                }
                else
                {
                    string id = _existingBuilding["building_id"]?.ToString() ?? "";
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/BUILDINGS?building_id=eq.{id}")
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

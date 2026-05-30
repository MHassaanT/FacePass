using System.Net.Http;
using System.Text;
using System.Windows;
using FacePass.Management.Services;
using Newtonsoft.Json.Linq;

namespace FacePass.Management.Views
{
    public partial class ClassroomDialog : Window
    {
        private readonly JObject? _existingClassroom;
        private JArray _buildings = new();

        public ClassroomDialog(JObject? existingClassroom = null)
        {
            InitializeComponent();
            _existingClassroom = existingClassroom;

            if (_existingClassroom != null)
            {
                Title = "Edit Classroom";
                RoomNumberBox.Text = _existingClassroom["room_number"]?.ToString();
                CapacityBox.Text = _existingClassroom["capacity"]?.ToString();
            }

            _ = LoadBuildingsAsync();
        }

        private async Task LoadBuildingsAsync()
        {
            try
            {
                using var client = SupabaseRestClient.Create();
                var resp = await client.GetAsync(
                    $"{SupabaseRestClient.BaseUrl}/rest/v1/BUILDINGS?select=*&order=building_name.asc");
                resp.EnsureSuccessStatusCode();

                _buildings = JArray.Parse(await resp.Content.ReadAsStringAsync());
                foreach (JObject building in _buildings)
                    building["name"] = building["building_name"];

                BuildingCombo.ItemsSource = _buildings;

                if (_existingClassroom != null &&
                    long.TryParse(_existingClassroom["building_id"]?.ToString(), out var buildingId))
                {
                    foreach (JObject building in _buildings)
                    {
                        if (long.TryParse(building["building_id"]?.ToString(), out var currentId) &&
                            currentId == buildingId)
                        {
                            BuildingCombo.SelectedItem = building;
                            break;
                        }
                    }
                }

                if (BuildingCombo.SelectedItem == null && BuildingCombo.Items.Count > 0)
                    BuildingCombo.SelectedIndex = 0;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading buildings: {ex.Message}");
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!long.TryParse(RoomNumberBox.Text, out var roomNumber))
            {
                MessageBox.Show("Please enter a valid room number.");
                return;
            }

            if (BuildingCombo.SelectedItem is not JObject selectedBuilding ||
                !long.TryParse(selectedBuilding["building_id"]?.ToString(), out var buildingId))
            {
                MessageBox.Show("Please select a building.");
                return;
            }

            long? capacity = null;
            if (!string.IsNullOrWhiteSpace(CapacityBox.Text))
            {
                if (!long.TryParse(CapacityBox.Text, out var parsedCapacity))
                {
                    MessageBox.Show("Please enter a valid capacity or leave it blank.");
                    return;
                }
                capacity = parsedCapacity;
            }

            try
            {
                using var client = SupabaseRestClient.Create();
                var payload = new JObject
                {
                    ["room_number"] = roomNumber,
                    ["building_id"] = buildingId,
                    ["capacity"] = capacity is null ? JValue.CreateNull() : capacity.Value
                };

                HttpResponseMessage resp;
                if (_existingClassroom == null)
                {
                    var request = new HttpRequestMessage(HttpMethod.Post,
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/CLASSROOMS")
                    {
                        Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("Prefer", "return=representation");
                    resp = await client.SendAsync(request);
                }
                else
                {
                    string id = _existingClassroom["classroom_id"]?.ToString() ?? "";
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                        $"{SupabaseRestClient.BaseUrl}/rest/v1/CLASSROOMS?classroom_id=eq.{id}")
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

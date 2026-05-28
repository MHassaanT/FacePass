using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using System.Threading.Tasks;

namespace FacePass.Kiosk.Services
{
    public class LocationService
    {
        public async Task<(double latitude, double longitude)?> GetCurrentLocationAsync()
        {
            try
            {
                var accessStatus = await Geolocator.RequestAccessAsync();
                if (accessStatus == GeolocationAccessStatus.Allowed)
                {
                    var geolocator = new Geolocator { DesiredAccuracyInMeters = 50 };
                    var pos = await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5));
                    
                    if (pos?.Coordinate?.Point?.Position != null)
                    {
                        return (pos.Coordinate.Point.Position.Latitude, pos.Coordinate.Point.Position.Longitude);
                    }
                }
            }
            catch
            {
                // Ignore native geolocation failures
            }

            // Fallback to IP-based location if native fails or is denied
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetStringAsync("http://ip-api.com/json/");
                var json = Newtonsoft.Json.Linq.JObject.Parse(response);
                
                if (json["status"]?.ToString() == "success")
                {
                    double lat = (double)json["lat"]!;
                    double lon = (double)json["lon"]!;
                    return (lat, lon);
                }
            }
            catch
            {
                // Ignore fallback failures
            }

            return null;
        }
    }
}

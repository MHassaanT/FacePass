using System;
using Microsoft.Maui.Devices.Sensors;
using System.Threading.Tasks;

namespace FacePass.Mobile.Services
{
    public class GeofencingService
    {
        private const double ClassroomRadiusMeters = 20.0;

        /// <summary>
        /// Checks if the current location is within 20 meters of the specified classroom coordinates.
        /// </summary>
        public async Task<bool> IsWithinClassroomRange(double targetLat, double targetLng)
        {
            try
            {
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10)));

                if (location == null) return false;

                double distance = Location.CalculateDistance(location.Latitude, location.Longitude, targetLat, targetLng, DistanceUnits.Kilometers) * 1000;

                return distance <= ClassroomRadiusMeters;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Geofence] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Manual Haversine implementation if needed for more precision or specific projections.
        /// Distance returned in Meters.
        /// </summary>
        public double GetDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371e3; // Earth radius in meters
            var phi1 = lat1 * Math.PI / 180;
            var phi2 = lat2 * Math.PI / 180;
            var deltaPhi = (lat2 - lat1) * Math.PI / 180;
            var deltaLambda = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                    Math.Cos(phi1) * Math.Cos(phi2) *
                    Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }
    }
}

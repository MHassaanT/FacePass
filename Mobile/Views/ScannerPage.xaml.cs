using ZXing.Net.Maui;
using Newtonsoft.Json;
using FacePass.Mobile.Services;

namespace FacePass.Mobile.Views
{
    public partial class ScannerPage : ContentPage
    {
        private readonly GeofencingService _geofence;
        private readonly SupabaseMobileService _supabase;
        private bool _isProcessing = false;

        // Mock Classroom Data - In production, this would be fetched from the API based on current time
        private const double TargetLat = 40.7128; 
        private const double TargetLng = -74.0060;
        private readonly Guid _courseId = Guid.NewGuid(); // Placeholder
        private readonly Guid _classroomId = Guid.NewGuid(); // Placeholder

        public ScannerPage(GeofencingService geofence, SupabaseMobileService supabase)
        {
            InitializeComponent();
            _geofence = geofence;
            _supabase = supabase;

            BarcodeReader.Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormat.QrCode,
                AutoRotate = true,
                Multiple = false
            };

            StartGeofenceMonitoring();
        }

        private async void StartGeofenceMonitoring()
        {
            while (true)
            {
                bool inRange = await _geofence.IsWithinClassroomRange(TargetLat, TargetLng);
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    GeofenceStatusBorder.BackgroundColor = inRange ? Color.FromArgb("#2E7D32") : Color.FromArgb("#C62828");
                    GeofenceStatusLabel.Text = inRange ? "✅ Inside Classroom Range" : "⚠️ Outside Classroom Range";
                    BarcodeReader.IsDetecting = inRange; // Disable scanner if out of range
                });

                await Task.Delay(5000); // Check every 5 seconds
            }
        }

        private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            if (_isProcessing) return;
            _isProcessing = true;

            var result = e.Results.FirstOrDefault();
            if (result == null) 
            {
                _isProcessing = false;
                return;
            }

            try
            {
                // 1. Parse Payload
                var payload = JsonConvert.DeserializeObject<dynamic>(result.Value);
                string sessionGuid = payload.session_id;
                DateTime expiresAt = payload.expires_at;

                // 2. Validate Expiry
                if (DateTime.UtcNow > expiresAt)
                {
                    await DisplayAlert("Error", "QR Code has expired.", "OK");
                    _isProcessing = false;
                    return;
                }

                // 3. Log Attendance
                // In a real app, studentId comes from the logged-in user session
                Guid studentId = Guid.NewGuid(); 

                await _supabase.LogAttendance(studentId, _courseId, _classroomId, "qr", "present");

                await DisplayAlert("Success", "Attendance marked successfully!", "OK");
                
                // Navigate back or to dashboard
                await Shell.Current.GoToAsync("///DashboardPage");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Invalid QR Code format.", "OK");
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}

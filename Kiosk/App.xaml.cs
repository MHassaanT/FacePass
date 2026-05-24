using System;
using System.Windows;
using FacePass.Kiosk.Services;
using FacePass.Kiosk.ViewModels;
using FacePass.Kiosk.Views;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace FacePass.Kiosk
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Load config
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                var supabaseUrl  = config["Supabase:Url"];
                var supabaseKey  = config["Supabase:AnonKey"];
                
                if (string.IsNullOrEmpty(supabaseUrl) || supabaseUrl.Contains("YOUR-PROJECT-REF"))
                {
                    MessageBox.Show("Warning: Supabase URL is not configured in appsettings.json. The application will start but biometric features may fail.", "Configuration Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                var classroomId = long.TryParse(config["Kiosk:ClassroomId"], out var cid) ? cid : 0L;
                var courseId    = long.TryParse(config["Kiosk:CourseId"], out var cosid) ? cosid : 0L;

                // Check for Haar Cascades
                string faceXml = Path.Combine(AppContext.BaseDirectory, "haarcascade_frontalface_default.xml");
                string eyeXml = Path.Combine(AppContext.BaseDirectory, "haarcascade_eye.xml");

                if (!File.Exists(faceXml) || !File.Exists(eyeXml))
                {
                    // If not in bin, check project root (for development)
                    if (!File.Exists(faceXml)) faceXml = "haarcascade_frontalface_default.xml";
                    if (!File.Exists(eyeXml)) eyeXml = "haarcascade_eye.xml";

                    if (!File.Exists(faceXml))
                    {
                        throw new FileNotFoundException("Haar Cascade file missing: haarcascade_frontalface_default.xml. Please ensure it is in the application directory.");
                    }
                }

                // Wire up services (manual DI)
                var supabaseService = new SupabaseService(supabaseUrl ?? "", supabaseKey ?? "");
                var faceRepo        = new SupabaseFaceRepository(supabaseService);
                var camera          = new CameraService();
                var detector        = new FaceDetectionService(faceXml);
                var encoder         = new FaceEncodingService();
                var liveness        = new LivenessChallengeService(eyeXml);
                var qrService       = new QrSessionService(faceRepo, classroomId);
                var attendance      = new AttendanceService(faceRepo);

                var vm = new MainWindowViewModel(
                    camera, detector, encoder, faceRepo,
                    liveness, qrService, attendance,
                    classroomId, courseId);

                var window = new MainWindow(vm);
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application failed to start:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}

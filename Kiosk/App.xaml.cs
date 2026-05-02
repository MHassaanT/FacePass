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

            // Load config
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var supabaseUrl  = config["Supabase:Url"]         ?? throw new Exception("Supabase:Url missing");
            var supabaseKey  = config["Supabase:AnonKey"]     ?? throw new Exception("Supabase:AnonKey missing");
            var classroomId  = Guid.Parse(config["Kiosk:ClassroomId"] ?? throw new Exception("Kiosk:ClassroomId missing"));
            var courseId     = Guid.Parse(config["Kiosk:CourseId"]    ?? throw new Exception("Kiosk:CourseId missing"));

            // Wire up services (manual DI)
            var supabaseService = new SupabaseService(supabaseUrl, supabaseKey);
            var faceRepo        = new SupabaseFaceRepository(supabaseService);
            var camera          = new CameraService();
            var detector        = new FaceDetectionService();
            var encoder         = new FaceEncodingService();
            var liveness        = new LivenessChallengeService();
            var qrService       = new QrSessionService(faceRepo, classroomId);
            var attendance      = new AttendanceService(faceRepo);

            var vm = new MainWindowViewModel(
                camera, detector, encoder, faceRepo,
                liveness, qrService, attendance,
                classroomId, courseId);

            var window = new MainWindow(vm);
            window.Show();
        }
    }
}

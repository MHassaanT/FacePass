using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;
using FacePass.Mobile.Services;
using FacePass.Mobile.Views;

namespace FacePass.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader() // Initialize ZXing
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Register Services
            builder.Services.AddSingleton<GeofencingService>();
            builder.Services.AddSingleton(new SupabaseMobileService(
                "https://mfcyozrkizrbrtpfihdj.supabase.co",
                "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im1mY3lvenJraXpyYnJ0cGZpaGRqIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzcwMjcwNDMsImV4cCI6MjA5MjYwMzA0M30.HHuB-oJs4TYEWMZi-7Loe3-cJHjLH8nvnGkBBaliJIE"
            ));

            // Register Pages
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<ScannerPage>();
            builder.Services.AddTransient<ProfilePage>();

            return builder.Build();
        }
    }
}

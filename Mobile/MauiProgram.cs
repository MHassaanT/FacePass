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
                "https://YOUR-PROJECT.supabase.co", 
                "YOUR-ANON-KEY"
            ));

            // Register Pages
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<ScannerPage>();
            builder.Services.AddTransient<ProfilePage>();

            return builder.Build();
        }
    }
}

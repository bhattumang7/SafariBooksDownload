using Microsoft.Extensions.Logging;
using Telerik.Maui.Controls.Compatibility;

namespace SafariBooksDownload
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseTelerik()
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                ;
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Register the global exception handler
            var logger = app.Services.GetRequiredService<ILogger<GlobalExceptionHandler>>();
            new GlobalExceptionHandler(logger);

            return app;
        }
    }
}

using Microsoft.Extensions.Logging;

namespace SafariBooksDownload
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
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

            builder.Services.AddSingleton<GlobalExceptionHandler>();

            var app = builder.Build();

            // Resolving the singleton registers the global exception handler
            app.Services.GetRequiredService<GlobalExceptionHandler>();

            return app;
        }
    }
}

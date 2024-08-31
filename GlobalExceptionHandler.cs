using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;


namespace SafariBooksDownload
{
    public class GlobalExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            _logger.LogError(exception, "Unhandled exception occurred");
            Debug.WriteLine($"Unhandled exception: {exception?.Message}");
        }
    }
}

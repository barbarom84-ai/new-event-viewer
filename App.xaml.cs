using System;
using System.Windows;
using System.Windows.Threading;

namespace EventViewer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings = AppSettingsStore.Load();
            TelemetryService.Configure(settings.TelemetryOptIn);
            TelemetryService.Track("app_start", new
            {
                storeBuild = IsStoreBuild()
            });

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        }

        private static bool IsStoreBuild()
        {
#if STORE_BUILD
            return true;
#else
            return false;
#endif
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                TelemetryService.TrackException("dispatcher_unhandled_exception", e.Exception);
            }
        }

        private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                TelemetryService.TrackException("domain_unhandled_exception", ex);
            }
        }
    }
}


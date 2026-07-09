using System.Diagnostics.CodeAnalysis;
using System.IO;
using EventViewer.Core;
using EventViewer.WinUI.Themes;
using EventViewer.WinUI.ViewModels;
using EventViewer.WinUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace EventViewer.WinUI;

public partial class App : Application
{
    private Window? _window;
    public static Window? MainWindow { get; private set; }
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        try
        {
            var settings = AppSettingsStore.Load();
            Loc.Initialize(settings.UiLanguage);
            InitializeComponent();
            // Do NOT touch Application.Resources here — WinUI throws E_UNEXPECTED
            // until the Application is fully constructed. Theme is applied in OnLaunched.
            Services = ConfigureServices();
            UnhandledException += OnUnhandledException;
        }
        catch (Exception ex)
        {
            WriteStartupCrash("App ctor", ex);
            throw;
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ErrorAnalyzer>();
        services.AddSingleton<TagDetector>();
        services.AddSingleton<InsightsService>();
        services.AddSingleton<IncidentTimelineService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<SnapshotManager>();
        services.AddSingleton<AiAssistantService>();
        services.AddSingleton<AutoFixService>();
        services.AddSingleton<IEventLogService, WindowsEventLogService>();
        services.AddTransient<MainViewModel>();
        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        try
        {
            _window ??= new Window
            {
                Title = Loc.T("App.Title")
            };
            MainWindow = _window;

            if (_window.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                _window.Content = rootFrame;
            }

            var settings = AppSettingsStore.Load();
            AppThemeService.Apply(settings.UiTheme, rootFrame);

            if (rootFrame.Content is null)
            {
                if (!rootFrame.Navigate(typeof(MainPage), e.Arguments))
                {
                    WriteStartupCrash("Navigate returned false", null);
                }
            }

            _window.Activate();
        }
        catch (Exception ex)
        {
            WriteStartupCrash("OnLaunched", ex);
            throw;
        }
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        WriteStartupCrash("UnhandledException", e.Exception);
        if (e.Exception != null)
        {
            TelemetryService.TrackException("winui_unhandled_exception", e.Exception);
        }
    }

    internal static void WriteStartupCrash(string stage, Exception? ex)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinBeacon",
                "startup-crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var text =
                $"[{DateTime.Now:O}] {stage}\n" +
                (ex == null ? "(no exception)\n" : ex + "\n") +
                "----\n";
            File.AppendAllText(path, text);
        }
        catch
        {
            // ignore logging failures
        }
    }

    [DoesNotReturn]
    private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        WriteStartupCrash("NavigationFailed " + e.SourcePageType.FullName, e.Exception);
        throw new Exception("Failed to load Page " + e.SourcePageType.FullName, e.Exception);
    }
}

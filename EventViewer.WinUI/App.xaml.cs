using System.Diagnostics.CodeAnalysis;
using EventViewer.Core;
using EventViewer.WinUI.ViewModels;
using EventViewer.WinUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace EventViewer.WinUI;

public partial class App : Application
{
    private Window? _window;
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
        UnhandledException += OnUnhandledException;
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
        _window ??= new Window
        {
            Title = "Observateur d'événements"
        };

        if (_window.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            _window.Content = rootFrame;
        }

        if (rootFrame.Content is null)
        {
            _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
        }

        _window.Activate();
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (e.Exception != null)
        {
            TelemetryService.TrackException("winui_unhandled_exception", e.Exception);
        }
    }

    [DoesNotReturn]
    private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        => throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
}

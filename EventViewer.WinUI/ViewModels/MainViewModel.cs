using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventViewer.Core;
using EventViewer.WinUI.Themes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EventViewer.WinUI.ViewModels;

public sealed class LogOption
{
    public required string DisplayName { get; set; }
    public required string LogName { get; init; }
}

public sealed class LanguageOption
{
    public required string Code { get; init; }
    public required string DisplayName { get; set; }
}

public partial class MainViewModel : ObservableObject
{
    private readonly IEventLogService _eventLogService;
    private readonly InsightsService _insightsService;
    private readonly IncidentTimelineService _timelineService;
    private readonly ExportService _exportService;
    private readonly SnapshotManager _snapshotManager;
    private readonly AiAssistantService _aiAssistantService;
    private readonly AutoFixService _autoFixService;
    private readonly AppSettings _settings;
    private readonly List<EventItem> _allEvents = [];
    private CancellationTokenSource? _loadCts;
    private bool _comparisonMode;
    private bool _isHydratingSettings;
    private HashSet<string> _newEventKeys = [];
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _filterDebounceTimer;
    private string? _lastMaintenanceActionKey;
    private CommandResult? _lastMaintenanceResult;
    private int _lastLoadedCount;

    public MainViewModel(
        IEventLogService eventLogService,
        InsightsService insightsService,
        IncidentTimelineService timelineService,
        ExportService exportService,
        SnapshotManager snapshotManager,
        AiAssistantService aiAssistantService,
        AutoFixService autoFixService)
    {
        _eventLogService = eventLogService;
        _insightsService = insightsService;
        _timelineService = timelineService;
        _exportService = exportService;
        _snapshotManager = snapshotManager;
        _aiAssistantService = aiAssistantService;
        _autoFixService = autoFixService;
        _settings = AppSettingsStore.Load();
        Loc.Initialize(_settings.UiLanguage);
        TelemetryService.Configure(_settings.TelemetryOptIn);

        Detail = new EventDetailViewModel();
        IsAdmin = AdminStatus.IsElevated();
        IsStoreBuild = DetectStoreBuild();
        ShowMaintenance = !IsStoreBuild;

        var queue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _filterDebounceTimer = queue.CreateTimer();
        _filterDebounceTimer.Interval = TimeSpan.FromMilliseconds(220);
        _filterDebounceTimer.IsRepeating = false;
        _filterDebounceTimer.Tick += (_, _) => ApplyFilter();

        LanguageOptions =
        [
            new LanguageOption { Code = AppLanguage.System, DisplayName = AppLanguage.DisplayName(AppLanguage.System) },
            new LanguageOption { Code = AppLanguage.French, DisplayName = "Français" },
            new LanguageOption { Code = AppLanguage.English, DisplayName = "English" },
            new LanguageOption { Code = AppLanguage.Italian, DisplayName = "Italiano" }
        ];
        ThemeOptions = AppThemeService.CreateOptions().ToList();

        _isHydratingSettings = true;
        TelemetryOptIn = _settings.TelemetryOptIn;
        AiBetaEnabled = _settings.AiBetaEnabled;
        AiCloudDataConsent = _settings.AiCloudDataConsent;
        AiEndpoint = _settings.AiApiEndpoint ?? string.Empty;
        AiModel = string.IsNullOrWhiteSpace(_settings.AiModel) ? "gpt-4o-mini" : _settings.AiModel;
        SelectedLanguage = LanguageOptions.FirstOrDefault(l => l.Code == AppLanguage.Normalize(_settings.UiLanguage))
                           ?? LanguageOptions[0];
        SelectedTheme = ThemeOptions.FirstOrDefault(t => t.Id == AppThemeService.Normalize(_settings.UiTheme))
                        ?? ThemeOptions[0];
        _isHydratingSettings = false;

        RefreshLogOptions();
        _selectedLog = LogOptions[0];
        ApplyLocalizedChrome();
        // Theme is applied from App.OnLaunched once Application.Resources is ready.
        RefreshSnapshotStatus();
        RefreshToolsStatus();
        TelemetryService.Track("app_start", new Dictionary<string, string>
        {
            ["storeBuild"] = IsStoreBuild ? "true" : "false",
            ["lang"] = Loc.EffectiveLanguage,
            ["theme"] = AppThemeService.Normalize(_settings.UiTheme)
        });
    }

    public EventDetailViewModel Detail { get; }
    public UiStrings Strings => UiStrings.Instance;

    public ObservableCollection<IncidentCardViewModel> Incidents { get; } = [];
    public ObservableCollection<IncidentBucket> TimelineBuckets { get; } = [];
    public ObservableCollection<LogOption> LogOptions { get; } = [];
    public IReadOnlyList<LanguageOption> LanguageOptions { get; }
    public IReadOnlyList<ThemeOption> ThemeOptions { get; }

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    [ObservableProperty]
    private ThemeOption? _selectedTheme;

    [ObservableProperty]
    private LogOption? _selectedLog;

    [ObservableProperty]
    private IncidentCardViewModel? _selectedIncident;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isBusyMaintenance;

    public bool CanRunMaintenance => !IsBusyMaintenance;

    public bool CanExecuteAutoFix => !IsBusyMaintenance && Detail.CanRunAutoFix;

    partial void OnIsBusyMaintenanceChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRunMaintenance));
        OnPropertyChanged(nameof(CanExecuteAutoFix));
    }

    [ObservableProperty]
    private bool _isAdmin;

    public bool NeedsAdminElevation => !IsAdmin;

    [ObservableProperty]
    private bool _isStoreBuild;

    [ObservableProperty]
    private bool _showMaintenance;

    [ObservableProperty]
    private string _statusMessage = Loc.T("Status.Ready");

    [ObservableProperty]
    private string _healthHeadline = Loc.T("Status.Loading");

    [ObservableProperty]
    private string _healthSubtitle = Loc.T("Status.Loading");

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private int _warningCount;

    [ObservableProperty]
    private int _riskScore;

    [ObservableProperty]
    private string _exportMessage = string.Empty;

    [ObservableProperty]
    private bool _hasExportMessage;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private string _emptyTitle = Loc.T("Empty.NoIncidents.Title");

    [ObservableProperty]
    private string _emptyMessage = Loc.T("Empty.NoIncidents.Message");

    [ObservableProperty]
    private string _snapshotStatus = Loc.T("Snapshot.None");

    [ObservableProperty]
    private bool _hasSnapshot;

    [ObservableProperty]
    private bool _comparisonActive;

    [ObservableProperty]
    private string _comparisonLabel = string.Empty;

    [ObservableProperty]
    private bool _telemetryOptIn;

    [ObservableProperty]
    private bool _aiBetaEnabled;

    [ObservableProperty]
    private bool _aiCloudDataConsent;

    [ObservableProperty]
    private string _toolsStatus = string.Empty;

    [ObservableProperty]
    private string _aiCloudStatus = Loc.T("Cloud.Status.NotConfigured");

    [ObservableProperty]
    private string _aiEndpoint = string.Empty;

    [ObservableProperty]
    private string _aiModel = "gpt-4o-mini";

    [ObservableProperty]
    private string _maintenanceMessage = string.Empty;

    [ObservableProperty]
    private bool _hasMaintenanceMessage;

    partial void OnSearchTextChanged(string value)
    {
        _filterDebounceTimer.Stop();
        _filterDebounceTimer.Start();
    }

    partial void OnSelectedLogChanged(LogOption? value)
    {
        if (value != null)
        {
            _ = LoadAsync();
        }
    }

    partial void OnSelectedIncidentChanged(IncidentCardViewModel? value)
    {
        if (value == null)
        {
            Detail.Clear();
        }
        else
        {
            var fix = ResolveAutoFix(value.Item);
            Detail.Load(value.Item, fix);
            OnPropertyChanged(nameof(CanExecuteAutoFix));
        }
    }

    public AutoFixRecommendation? PendingAutoFix => Detail.CurrentAutoFix;

    private AutoFixRecommendation ResolveAutoFix(EventItem item)
        => _autoFixService.Recommend(item, IsStoreBuild);

    // Confirmation gate is in MainPage (ContentDialog) → ExecuteConfirmedAutoFixAsync.

    public async Task ExecuteConfirmedAutoFixAsync(AutoFixRecommendation recommendation)
    {
        IsBusyMaintenance = true;
        var progress = recommendation.IsLongRunning
            ? Loc.T("Action.InProgressLong", recommendation.Title)
            : Loc.T("Action.InProgress", recommendation.Title);
        if (recommendation.Kind is AutoFixKind.RunSfc)
        {
            progress = Loc.T("Action.InProgressLong", recommendation.Title) + "\n" + Loc.T("Action.SfcConsoleHint");
        }

        Detail.SetAutoFixResult(progress);
        StatusMessage = progress;
        MaintenanceMessage = progress;
        HasMaintenanceMessage = true;

        try
        {
            var result = await _autoFixService.ExecuteAsync(recommendation.Kind);
            var summary = result.GetUserSummary(recommendation.Title);
            if (recommendation.Kind is AutoFixKind.ResetNetwork && result.Success)
            {
                summary += Loc.T("Action.RebootMayBeRequired");
            }

            Detail.SetAutoFixResult(summary);
            StatusMessage = summary;
            MaintenanceMessage = summary;
            HasMaintenanceMessage = true;
            TelemetryService.Track("auto_fix_executed", new Dictionary<string, string>
            {
                ["kind"] = recommendation.Kind.ToString(),
                ["success"] = result.Success ? "true" : "false",
                ["eventId"] = SelectedIncident?.Item.EventId.ToString() ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            var msg = Loc.T("Action.Failed", recommendation.Title, ex.Message);
            Detail.SetAutoFixResult(msg);
            StatusMessage = msg;
            MaintenanceMessage = msg;
            HasMaintenanceMessage = true;
            TelemetryService.TrackException("auto_fix_failed", ex);
        }
        finally
        {
            IsBusyMaintenance = false;
        }
    }

    partial void OnTelemetryOptInChanged(bool value)
    {
        if (_isHydratingSettings)
        {
            return;
        }

        _settings.TelemetryOptIn = value;
        if (!PersistSettings())
        {
            return;
        }

        TelemetryService.Configure(value);
        RefreshToolsStatus();
        TelemetryService.Track("telemetry_toggled", new Dictionary<string, string> { ["enabled"] = value ? "true" : "false" });
        StatusMessage = value ? Loc.T("Status.TelemetryOn") : Loc.T("Status.TelemetryOff");
    }

    partial void OnAiBetaEnabledChanged(bool value)
    {
        if (_isHydratingSettings)
        {
            return;
        }

        _settings.AiBetaEnabled = value;
        if (!value)
        {
            AiCloudDataConsent = false;
            _settings.AiCloudDataConsent = false;
        }

        if (!PersistSettings())
        {
            return;
        }

        RefreshToolsStatus();
        TelemetryService.Track("ai_beta_toggled", new Dictionary<string, string> { ["enabled"] = value ? "true" : "false" });
        StatusMessage = value ? Loc.T("Status.AiBetaOn") : Loc.T("Status.AiBetaOff");
    }

    partial void OnAiCloudDataConsentChanged(bool value)
    {
        if (_isHydratingSettings)
        {
            return;
        }

        _settings.AiCloudDataConsent = value;
        if (!PersistSettings())
        {
            return;
        }

        RefreshToolsStatus();
        TelemetryService.Track("ai_cloud_consent_toggled", new Dictionary<string, string>
        {
            ["enabled"] = value ? "true" : "false"
        });
        StatusMessage = value
            ? Loc.T("Status.CloudConsentOn")
            : Loc.T("Status.CloudConsentOff");
    }

    partial void OnAiEndpointChanged(string value)
    {
        if (_isHydratingSettings)
        {
            return;
        }

        var trimmed = value?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(trimmed) && !AiEndpointPolicy.IsAllowed(trimmed))
        {
            AiEndpointPolicy.TryValidate(trimmed, out _, out var error);
            StatusMessage = Loc.T("Status.AiEndpointRejected", error);
            RefreshToolsStatus();
            // Still persist so the user sees the value, but cloud calls will refuse it.
        }

        _settings.AiApiEndpoint = trimmed;
        PersistSettings();
        RefreshToolsStatus();
    }

    partial void OnAiModelChanged(string value)
    {
        if (_isHydratingSettings)
        {
            return;
        }

        _settings.AiModel = string.IsNullOrWhiteSpace(value) ? "gpt-4o-mini" : value.Trim();
        PersistSettings();
        RefreshToolsStatus();
    }

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (_isHydratingSettings || value == null)
        {
            return;
        }

        _settings.UiLanguage = value.Code;
        if (!PersistSettings())
        {
            return;
        }

        Loc.Apply(value.Code);
        ApplyLocalizedChrome();
        RefreshLogOptions();
        RefreshSnapshotStatus();
        RefreshToolsStatus();
        RefreshInsights();
        RefreshLocalizedRuntimeMessages();
        ApplyFilter();
        if (SelectedIncident != null)
        {
            Detail.Load(SelectedIncident.Item, ResolveAutoFix(SelectedIncident.Item));
        }

        if (App.MainWindow != null)
        {
            App.MainWindow.Title = Loc.T("App.Title");
        }

        StatusMessage = Loc.T("Lang.RestartHint");
        TelemetryService.Track("language_changed", new Dictionary<string, string>
        {
            ["lang"] = Loc.EffectiveLanguage,
            ["preference"] = value.Code
        });
    }

    partial void OnSelectedThemeChanged(ThemeOption? value)
    {
        if (_isHydratingSettings || value == null)
        {
            return;
        }

        _settings.UiTheme = value.Id;
        if (!PersistSettings())
        {
            return;
        }

        FrameworkElement? root = null;
        if (App.MainWindow?.Content is Frame frame)
        {
            root = frame.Content as FrameworkElement ?? frame;
        }

        AppThemeService.Apply(value.Id, root);
        StatusMessage = Loc.T("Theme.Applied", value.DisplayName);
        TelemetryService.Track("theme_changed", new Dictionary<string, string>
        {
            ["theme"] = value.Id
        });
    }

    private void RefreshLogOptions()
    {
        var selectedName = SelectedLog?.LogName;
        LogOptions.Clear();
        LogOptions.Add(new LogOption { DisplayName = Loc.T("Log.System"), LogName = "System" });
        LogOptions.Add(new LogOption { DisplayName = Loc.T("Log.Application"), LogName = "Application" });
        LogOptions.Add(new LogOption { DisplayName = Loc.T("Log.Security"), LogName = "Security" });
        SelectedLog = LogOptions.FirstOrDefault(l => l.LogName == selectedName) ?? LogOptions[0];
    }

    private void ApplyLocalizedChrome()
    {
        OnPropertyChanged(nameof(Strings));
        LanguageOptions[0].DisplayName = AppLanguage.DisplayName(AppLanguage.System);
        foreach (var theme in ThemeOptions)
        {
            theme.DisplayName = AppThemeService.DisplayName(theme.Id);
        }

        if (ErrorCount == 0 && WarningCount == 0 && !IsLoading)
        {
            HealthSubtitle = Loc.T("Health.SubtitleOk");
        }
        else if (!IsLoading)
        {
            HealthSubtitle = Loc.T("Health.SubtitleStats", ErrorCount, WarningCount, RiskScore);
        }
    }

    private bool PersistSettings()
    {
        if (!AppSettingsStore.TrySave(_settings, out var error))
        {
            StatusMessage = Loc.T("Status.SettingsSaveFailed", error ?? string.Empty);
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        IsLoading = true;
        StatusMessage = Loc.T("Status.LoadingIncidents");
        ExportMessage = string.Empty;
        HasExportMessage = false;

        try
        {
            var logName = SelectedLog?.LogName ?? "System";
            var events = await _eventLogService.LoadRecentAsync(logName, 200, token);

            _allEvents.Clear();
            _allEvents.AddRange(events);

            if (_comparisonMode)
            {
                ApplyComparisonMarkers();
            }

            RefreshInsights();
            ApplyFilter();

            _lastLoadedCount = events.Count;
            StatusMessage = IsAdmin
                ? Loc.T("Status.UpdatedCount", events.Count)
                : Loc.T("Status.LimitedWithoutAdmin");

            TelemetryService.Track("events_loaded", new Dictionary<string, string>
            {
                ["count"] = events.Count.ToString(),
                ["log"] = logName
            });
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Loc.T("Status.LoadCancelled");
        }
        catch (Exception ex)
        {
            TelemetryService.TrackException("events_load_failed", ex);
            _allEvents.Clear();
            Incidents.Clear();
            IsEmpty = true;
            EmptyTitle = Loc.T("Empty.LoadFailed.Title");
            EmptyMessage = IsAdmin
                ? Loc.T("Empty.LoadFailed.Message", ex.Message)
                : Loc.T("Empty.LoadFailed.AdminRequired");
            HealthHeadline = Loc.T("Health.ReadFailed");
            HealthSubtitle = EmptyMessage;
            StatusMessage = Loc.T("Status.ReadError");
            Detail.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private void ToggleTechnical() => Detail.ToggleTechnical();

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        try
        {
            if (_allEvents.Count == 0)
            {
                StatusMessage = Loc.T("Export.NoData");
                ExportMessage = StatusMessage;
                HasExportMessage = true;
                return;
            }

            var directory = ResolveWritableExportDirectory();
            var path = await _exportService.ExportCsvAsync(_allEvents, directory);
            ExportMessage = Loc.T("Export.CsvPath", path);
            HasExportMessage = true;
            StatusMessage = Loc.T("Export.CsvDone");
            TelemetryService.Track("export_csv");
        }
        catch (Exception ex)
        {
            ExportMessage = Loc.T("Export.CsvFailed", ex.Message);
            HasExportMessage = true;
            StatusMessage = ExportMessage;
        }
    }

    [RelayCommand]
    private async Task ExportJsonAsync()
    {
        try
        {
            if (_allEvents.Count == 0)
            {
                StatusMessage = Loc.T("Export.NoData");
                ExportMessage = StatusMessage;
                HasExportMessage = true;
                return;
            }

            var directory = ResolveWritableExportDirectory();
            var insights = _insightsService.Build(_allEvents);
            var path = await _exportService.ExportJsonAsync(_allEvents, insights, directory);
            ExportMessage = Loc.T("Export.JsonPath", path);
            HasExportMessage = true;
            StatusMessage = Loc.T("Export.JsonDone");
            TelemetryService.Track("export_json");
        }
        catch (Exception ex)
        {
            ExportMessage = Loc.T("Export.JsonFailed", ex.Message);
            HasExportMessage = true;
            StatusMessage = ExportMessage;
        }
    }

    [RelayCommand]
    private void OpenExportFolder()
    {
        try
        {
            var directory = ResolveWritableExportDirectory();
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(directory);
            }

            // Only open our own export directory — never pass untrusted paths to shell.
            var full = Path.GetFullPath(directory);
            var allowedRoot = Path.GetFullPath(AppPaths.ExportDirectory);
            var allowedFallback = Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinBeacon",
                "Exports"));

            if (!full.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) &&
                !full.StartsWith(allowedFallback, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(Loc.T("Export.FolderNotAllowed"));
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = full,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ExportMessage = Loc.T("Export.OpenFolderFailed", ex.Message);
            HasExportMessage = true;
            StatusMessage = ExportMessage;
        }
    }

    private static string ResolveWritableExportDirectory()
    {
        var directory = AppPaths.ExportDirectory;
        if (AppPaths.TryEnsureWritableDirectory(directory, out var error))
        {
            return directory;
        }

        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinBeacon",
            "Exports");
        if (AppPaths.TryEnsureWritableDirectory(fallback, out _))
        {
            return fallback;
        }

        throw new IOException(error ?? Loc.T("Export.FolderInaccessible"));
    }

    [RelayCommand]
    private void CreateSnapshot()
    {
        try
        {
            _snapshotManager.CreateSnapshot(_allEvents);
            _comparisonMode = false;
            ComparisonActive = false;
            ComparisonLabel = string.Empty;
            ClearNewMarkers();
            RefreshSnapshotStatus();
            ApplyFilter();
            StatusMessage = Loc.T("Snapshot.Saved");
            TelemetryService.Track("snapshot_created", new Dictionary<string, string>
            {
                ["count"] = _allEvents.Count.ToString()
            });
        }
        catch (Exception ex)
        {
            StatusMessage = Loc.T("Snapshot.SaveFailed", ex.Message);
        }
    }

    [RelayCommand]
    private void CompareSnapshot()
    {
        if (!_snapshotManager.SnapshotExists())
        {
            StatusMessage = Loc.T("Snapshot.NoneToCompare");
            return;
        }

        _newEventKeys = _snapshotManager.GetNewEventKeys(_allEvents);
        _comparisonMode = true;
        ComparisonActive = true;
        var date = _snapshotManager.GetSnapshotDate();
        ComparisonLabel = date.HasValue
            ? Loc.T("Snapshot.NewSinceDate", _newEventKeys.Count, date.Value.ToString("g", System.Globalization.CultureInfo.CurrentUICulture))
            : Loc.T("Snapshot.NewSinceSnapshot", _newEventKeys.Count);

        ApplyComparisonMarkers();
        ApplyFilter();
        StatusMessage = ComparisonLabel;
        TelemetryService.Track("snapshot_compared", new Dictionary<string, string>
        {
            ["newCount"] = _newEventKeys.Count.ToString()
        });
    }

    [RelayCommand]
    private void ClearComparison()
    {
        _comparisonMode = false;
        ComparisonActive = false;
        ComparisonLabel = string.Empty;
        _newEventKeys = [];
        ClearNewMarkers();
        ApplyFilter();
        StatusMessage = Loc.T("Snapshot.ComparisonCleared");
    }

    [RelayCommand]
    private async Task ExplainSelectedAsync()
    {
        if (SelectedIncident == null)
        {
            StatusMessage = Loc.T("Status.SelectIncidentFirst");
            return;
        }

        StatusMessage = Loc.T("Status.PreparingSummary");
        var summary = await _aiAssistantService.BuildIncidentSummaryAsync(SelectedIncident.Item, _settings);
        Detail.SetAssistantSummary(summary);
        StatusMessage = summary.StartsWith(Loc.T("Ai.CloudMode"), StringComparison.Ordinal)
            ? Loc.T("Status.SummaryCloudReady")
            : (AiBetaEnabled ? Loc.T("Status.SummaryBetaReady") : Loc.T("Status.SummaryLocalReady"));
        TelemetryService.Track("assistant_explain", new Dictionary<string, string>
        {
            ["aiBeta"] = AiBetaEnabled ? "true" : "false",
            ["cloud"] = AiAssistantService.IsCloudConfigured(_settings) && AiCloudDataConsent ? "true" : "false",
            ["eventId"] = SelectedIncident.Item.EventId.ToString()
        });
    }

    [RelayCommand]
    private void FeedbackUseful() => SaveFeedback(useful: true);

    [RelayCommand]
    private void FeedbackNotUseful() => SaveFeedback(useful: false);

    private void SaveFeedback(bool useful)
    {
        if (SelectedIncident == null)
        {
            StatusMessage = Loc.T("Status.SelectIncidentFirst");
            return;
        }

        try
        {
            RecommendationFeedbackStore.Add(SelectedIncident.Item, useful);
            var msg = useful ? Loc.T("Feedback.Useful") : Loc.T("Feedback.NotUseful");
            Detail.SetFeedbackMessage(msg);
            StatusMessage = msg;
        }
        catch (Exception ex)
        {
            StatusMessage = Loc.T("Feedback.SaveFailed", ex.Message);
            Detail.SetFeedbackMessage(StatusMessage);
        }
    }

    public Task ExecuteConfirmedFlushDnsAsync()
        => RunMaintenanceAsync(
            "Maintenance.Label.FlushDns",
            () => SystemMaintenanceHelper.FlushDnsAsync(),
            "flush_dns");

    public Task ExecuteConfirmedResetNetworkAsync()
        => RunMaintenanceAsync(
            "Maintenance.Label.ResetNetwork",
            () => SystemMaintenanceHelper.ResetNetworkAsync(),
            "reset_network");

    public Task ExecuteConfirmedRunSfcAsync()
        => RunMaintenanceAsync(
            "Maintenance.Label.Sfc",
            () => SystemMaintenanceHelper.RunSystemFileCheckerAsync(),
            "sfc_scan",
            consoleHintKey: "Action.SfcConsoleHint");

    private async Task RunMaintenanceAsync(
        string labelKey,
        Func<Task<CommandResult>> action,
        string telemetryName,
        string? consoleHintKey = null)
    {
        if (!ShowMaintenance)
        {
            return;
        }

        if (!IsAdmin)
        {
            MaintenanceMessage = Loc.T("Maintenance.AdminRequired");
            HasMaintenanceMessage = true;
            return;
        }

        var label = Loc.T(labelKey);
        IsBusyMaintenance = true;
        MaintenanceMessage = string.IsNullOrWhiteSpace(consoleHintKey)
            ? Loc.T("Action.InProgressLong", label)
            : Loc.T("Action.InProgressLong", label) + "\n" + Loc.T(consoleHintKey);
        HasMaintenanceMessage = true;
        StatusMessage = MaintenanceMessage;

        try
        {
            var result = await action();
            _lastMaintenanceActionKey = labelKey;
            _lastMaintenanceResult = result;
            MaintenanceMessage = result.GetUserSummary(Loc.T(labelKey));
            HasMaintenanceMessage = true;
            StatusMessage = BuildCombinedStatus(MaintenanceMessage);
            TelemetryService.Track(telemetryName, new Dictionary<string, string>
            {
                ["success"] = result.Success ? "true" : "false",
                ["exitCode"] = result.ExitCode.ToString()
            });
        }
        catch (Exception ex)
        {
            _lastMaintenanceActionKey = labelKey;
            _lastMaintenanceResult = null;
            MaintenanceMessage = Loc.T("Action.Failed", Loc.T(labelKey), ex.Message);
            HasMaintenanceMessage = true;
            StatusMessage = BuildCombinedStatus(MaintenanceMessage);
            TelemetryService.TrackException(telemetryName + "_failed", ex);
        }
        finally
        {
            IsBusyMaintenance = false;
        }
    }

    private void RefreshLocalizedRuntimeMessages()
    {
        if (_lastMaintenanceResult != null && !string.IsNullOrWhiteSpace(_lastMaintenanceActionKey))
        {
            MaintenanceMessage = _lastMaintenanceResult.GetUserSummary(Loc.T(_lastMaintenanceActionKey));
            HasMaintenanceMessage = true;
            StatusMessage = BuildCombinedStatus(MaintenanceMessage);
            return;
        }

        if (_lastLoadedCount > 0)
        {
            StatusMessage = IsAdmin
                ? Loc.T("Status.UpdatedCount", _lastLoadedCount)
                : Loc.T("Status.LimitedWithoutAdmin");
        }
    }

    private string BuildCombinedStatus(string maintenanceSummary)
    {
        if (_lastLoadedCount <= 0)
        {
            return maintenanceSummary;
        }

        var loadPart = IsAdmin
            ? Loc.T("Status.UpdatedCount", _lastLoadedCount)
            : Loc.T("Status.LimitedWithoutAdmin");
        return $"{loadPart} · {maintenanceSummary}";
    }

    private void RefreshInsights()
    {
        var insights = _insightsService.Build(_allEvents);
        HealthHeadline = insights.HealthHeadline;
        ErrorCount = insights.CriticalCount;
        WarningCount = insights.WarningCount;
        RiskScore = insights.RiskScore;
        HealthSubtitle = insights.CriticalCount == 0 && insights.WarningCount == 0
            ? Loc.T("Health.SubtitleOk")
            : Loc.T("Health.SubtitleStats", insights.CriticalCount, insights.WarningCount, insights.RiskScore);

        TimelineBuckets.Clear();
        foreach (var bucket in _timelineService.BuildLast24Hours(_allEvents))
        {
            TimelineBuckets.Add(bucket);
        }
    }

    private void RefreshSnapshotStatus()
    {
        HasSnapshot = _snapshotManager.SnapshotExists();
        var date = _snapshotManager.GetSnapshotDate();
        SnapshotStatus = HasSnapshot && date.HasValue
            ? Loc.T("Snapshot.Of", date.Value.ToString("g", System.Globalization.CultureInfo.CurrentUICulture))
            : Loc.T("Snapshot.None");
    }

    private void RefreshToolsStatus()
    {
        var telemetry = TelemetryOptIn ? "ON" : "OFF";
        var ai = AiBetaEnabled ? "ON" : "OFF";
        ToolsStatus = Loc.T("Options.ToolsStatus", telemetry, ai);

        if (!AiBetaEnabled)
        {
            AiCloudStatus = Loc.T("Cloud.Status.Disabled");
        }
        else if (!AiCloudDataConsent)
        {
            AiCloudStatus = Loc.T("Cloud.Status.ConsentRequired");
        }
        else if (AiAssistantService.IsCloudConfigured(_settings))
        {
            AiCloudStatus = Loc.T("Cloud.Status.Ready");
        }
        else if (!string.IsNullOrWhiteSpace(AiEndpoint) && !AiEndpointPolicy.IsAllowed(AiEndpoint))
        {
            AiCloudStatus = Loc.T("Cloud.Status.EndpointRejected");
        }
        else
        {
            AiCloudStatus = Loc.T("Cloud.Status.NotConfigured");
        }
    }

    private void ApplyComparisonMarkers()
    {
        foreach (var evt in _allEvents)
        {
            var key = SnapshotManager.CreateEventKey(evt.EventId, evt.Source, evt.Message);
            evt.IsNew = _newEventKeys.Contains(key);
        }
    }

    private void ClearNewMarkers()
    {
        foreach (var evt in _allEvents)
        {
            evt.IsNew = false;
        }
    }

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        IEnumerable<EventItem> filtered = _allEvents;

        if (_comparisonMode)
        {
            filtered = filtered.Where(e => e.IsNew);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(e =>
                Contains(e.ExplanationTitle, query) ||
                Contains(e.Message, query) ||
                Contains(e.Source, query) ||
                Contains(e.EventId.ToString(), query) ||
                Contains(e.Tag?.Name, query));
        }

        var list = filtered as List<EventItem> ?? filtered.ToList();

        // Skip UI churn when the visible set is unchanged.
        if (list.Count == Incidents.Count &&
            !list.Where((item, index) => !ReferenceEquals(item, Incidents[index].Item)).Any())
        {
            UpdateEmptyState(list.Count);
            return;
        }

        Incidents.Clear();
        foreach (var item in list)
        {
            Incidents.Add(new IncidentCardViewModel(item));
        }

        UpdateEmptyState(Incidents.Count);
        if (Incidents.Count == 0)
        {
            SelectedIncident = null;
            Detail.Clear();
        }
        else if (SelectedIncident == null || !Incidents.Any(i => ReferenceEquals(i.Item, SelectedIncident.Item)))
        {
            SelectedIncident = Incidents[0];
        }
    }

    private void UpdateEmptyState(int visibleCount)
    {
        IsEmpty = visibleCount == 0;
        if (!IsEmpty)
        {
            return;
        }

        if (_comparisonMode && _allEvents.Count > 0)
        {
            EmptyTitle = Loc.T("Empty.NoNew.Title");
            EmptyMessage = Loc.T("Empty.NoNew.Message");
        }
        else if (_allEvents.Count == 0)
        {
            EmptyTitle = Loc.T("Empty.NoIncidents.Title");
            EmptyMessage = Loc.T("Empty.NoIncidents.Message");
        }
        else
        {
            EmptyTitle = Loc.T("Empty.NoResults.Title");
            EmptyMessage = Loc.T("Empty.NoResults.Message");
        }
    }

    private static bool Contains(string? value, string query)
        => !string.IsNullOrEmpty(value) &&
           value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool DetectStoreBuild()
    {
#if STORE_BUILD
        return true;
#else
        return false;
#endif
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EventViewer.Core;

namespace EventViewer.WinUI.ViewModels;

public sealed class LogOption
{
    public required string DisplayName { get; init; }
    public required string LogName { get; init; }
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
        TelemetryService.Configure(_settings.TelemetryOptIn);

        Detail = new EventDetailViewModel();
        IsAdmin = AdminStatus.IsElevated();
        IsStoreBuild = DetectStoreBuild();
        ShowMaintenance = !IsStoreBuild;

        _isHydratingSettings = true;
        TelemetryOptIn = _settings.TelemetryOptIn;
        AiBetaEnabled = _settings.AiBetaEnabled;
        AiEndpoint = _settings.AiApiEndpoint ?? string.Empty;
        AiModel = string.IsNullOrWhiteSpace(_settings.AiModel) ? "gpt-4o-mini" : _settings.AiModel;
        _isHydratingSettings = false;

        _selectedLog = LogOptions[0];
        RefreshSnapshotStatus();
        RefreshToolsStatus();
        TelemetryService.Track("app_start", new Dictionary<string, string> { ["storeBuild"] = IsStoreBuild ? "true" : "false" });
    }

    public EventDetailViewModel Detail { get; }

    public ObservableCollection<IncidentCardViewModel> Incidents { get; } = [];
    public ObservableCollection<IncidentBucket> TimelineBuckets { get; } = [];

    public IReadOnlyList<LogOption> LogOptions { get; } =
    [
        new LogOption { DisplayName = "Système", LogName = "System" },
        new LogOption { DisplayName = "Applications", LogName = "Application" },
        new LogOption { DisplayName = "Sécurité", LogName = "Security" }
    ];

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
    private string _statusMessage = "Prêt";

    [ObservableProperty]
    private string _healthHeadline = "Analyse en cours…";

    [ObservableProperty]
    private string _healthSubtitle = "Lecture des journaux Windows";

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
    private string _emptyTitle = "Aucun incident récent";

    [ObservableProperty]
    private string _emptyMessage = "C'est une bonne nouvelle : aucune erreur ni avertissement dans ce journal.";

    [ObservableProperty]
    private string _snapshotStatus = "Aucun point de comparaison";

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
    private string _toolsStatus = string.Empty;

    [ObservableProperty]
    private string _aiCloudStatus = "Cloud IA : non configuré";

    [ObservableProperty]
    private string _aiEndpoint = string.Empty;

    [ObservableProperty]
    private string _aiModel = "gpt-4o-mini";

    [ObservableProperty]
    private string _maintenanceMessage = string.Empty;

    [ObservableProperty]
    private bool _hasMaintenanceMessage;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

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

    [RelayCommand]
    private async Task RunSuggestedFixAsync()
    {
        var recommendation = Detail.CurrentAutoFix;
        if (recommendation == null || !recommendation.CanExecuteAutomatically || recommendation.Kind == AutoFixKind.None)
        {
            StatusMessage = "Aucune correction automatique pour cet incident.";
            return;
        }

        if (recommendation.RequiresAdmin && !IsAdmin)
        {
            var msg = "Cette correction nécessite les droits administrateur. Relancez l'application en admin.";
            Detail.SetAutoFixResult(msg);
            StatusMessage = msg;
            return;
        }

        // Confirmation is handled by the view (ContentDialog) before calling ExecuteConfirmedAutoFixAsync.
        await ExecuteConfirmedAutoFixAsync(recommendation);
    }

    public async Task ExecuteConfirmedAutoFixAsync(AutoFixRecommendation recommendation)
    {
        IsBusyMaintenance = true;
        var progress = recommendation.IsLongRunning
            ? $"{recommendation.Title} en cours… Cela peut prendre plusieurs minutes."
            : $"{recommendation.Title} en cours…";
        Detail.SetAutoFixResult(progress);
        StatusMessage = progress;

        try
        {
            var result = await _autoFixService.ExecuteAsync(recommendation.Kind);
            var summary = result.GetUserSummary(recommendation.Title);
            if (recommendation.Kind is AutoFixKind.ResetNetwork && result.Success)
            {
                summary += " Un redémarrage peut être nécessaire.";
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
            var msg = $"{recommendation.Title} a échoué : {ex.Message}";
            Detail.SetAutoFixResult(msg);
            StatusMessage = msg;
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
        StatusMessage = value ? "Télémétrie locale activée" : "Télémétrie locale désactivée";
    }

    partial void OnAiBetaEnabledChanged(bool value)
    {
        if (_isHydratingSettings)
        {
            return;
        }

        _settings.AiBetaEnabled = value;
        if (!PersistSettings())
        {
            return;
        }

        RefreshToolsStatus();
        TelemetryService.Track("ai_beta_toggled", new Dictionary<string, string> { ["enabled"] = value ? "true" : "false" });
        StatusMessage = value ? "IA bêta activée" : "IA bêta désactivée";
    }

    partial void OnAiEndpointChanged(string value)
    {
        if (_isHydratingSettings)
        {
            return;
        }

        _settings.AiApiEndpoint = value?.Trim() ?? string.Empty;
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

    private bool PersistSettings()
    {
        if (!AppSettingsStore.TrySave(_settings, out var error))
        {
            StatusMessage = $"Impossible d'enregistrer les options : {error}";
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
        StatusMessage = "Chargement des incidents…";
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

            StatusMessage = IsAdmin
                ? $"Mis à jour · {events.Count} incident(s)"
                : "Certaines données peuvent être limitées sans droits administrateur";

            TelemetryService.Track("events_loaded", new Dictionary<string, string>
            {
                ["count"] = events.Count.ToString(),
                ["log"] = logName
            });
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Chargement annulé";
        }
        catch (Exception ex)
        {
            TelemetryService.TrackException("events_load_failed", ex);
            _allEvents.Clear();
            Incidents.Clear();
            IsEmpty = true;
            EmptyTitle = "Impossible de lire les journaux";
            EmptyMessage = IsAdmin
                ? $"Une erreur s'est produite : {ex.Message}"
                : "Lancez l'application en administrateur pour lire les journaux Windows.";
            HealthHeadline = "Lecture impossible";
            HealthSubtitle = EmptyMessage;
            StatusMessage = "Erreur de lecture";
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
                StatusMessage = "Aucun incident à exporter. Actualisez d'abord.";
                ExportMessage = StatusMessage;
                HasExportMessage = true;
                return;
            }

            var directory = ResolveWritableExportDirectory();
            var path = await _exportService.ExportCsvAsync(_allEvents, directory);
            ExportMessage = $"Export CSV : {path}";
            HasExportMessage = true;
            StatusMessage = "Export CSV terminé";
            TelemetryService.Track("export_csv");
        }
        catch (Exception ex)
        {
            ExportMessage = $"Échec de l'export CSV : {ex.Message}";
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
                StatusMessage = "Aucun incident à exporter. Actualisez d'abord.";
                ExportMessage = StatusMessage;
                HasExportMessage = true;
                return;
            }

            var directory = ResolveWritableExportDirectory();
            var insights = _insightsService.Build(_allEvents);
            var path = await _exportService.ExportJsonAsync(_allEvents, insights, directory);
            ExportMessage = $"Rapport JSON : {path}";
            HasExportMessage = true;
            StatusMessage = "Export JSON terminé";
            TelemetryService.Track("export_json");
        }
        catch (Exception ex)
        {
            ExportMessage = $"Échec de l'export JSON : {ex.Message}";
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
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ExportMessage = $"Impossible d'ouvrir le dossier : {ex.Message}";
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
            "EventBeaconTool",
            "Exports");
        if (AppPaths.TryEnsureWritableDirectory(fallback, out _))
        {
            return fallback;
        }

        throw new IOException(error ?? "Dossier d'export inaccessible.");
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
            StatusMessage = "Point de comparaison enregistré";
            TelemetryService.Track("snapshot_created", new Dictionary<string, string>
            {
                ["count"] = _allEvents.Count.ToString()
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Impossible d'enregistrer le snapshot : {ex.Message}";
        }
    }

    [RelayCommand]
    private void CompareSnapshot()
    {
        if (!_snapshotManager.SnapshotExists())
        {
            StatusMessage = "Aucun point de comparaison. Enregistrez-en un d'abord.";
            return;
        }

        _newEventKeys = _snapshotManager.GetNewEventKeys(_allEvents);
        _comparisonMode = true;
        ComparisonActive = true;
        var date = _snapshotManager.GetSnapshotDate();
        ComparisonLabel = date.HasValue
            ? $"{_newEventKeys.Count} nouveau(x) depuis le {date:dd/MM/yyyy HH:mm}"
            : $"{_newEventKeys.Count} nouveau(x) depuis le snapshot";

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
        StatusMessage = "Comparaison désactivée";
    }

    [RelayCommand]
    private async Task ExplainSelectedAsync()
    {
        if (SelectedIncident == null)
        {
            StatusMessage = "Sélectionnez un incident d'abord";
            return;
        }

        StatusMessage = "Préparation du résumé…";
        var summary = await _aiAssistantService.BuildIncidentSummaryAsync(SelectedIncident.Item, _settings);
        Detail.SetAssistantSummary(summary);
        StatusMessage = summary.StartsWith("Mode IA cloud", StringComparison.Ordinal)
            ? "Résumé cloud prêt"
            : (AiBetaEnabled ? "Résumé IA bêta (local) prêt" : "Résumé local prêt");
        TelemetryService.Track("assistant_explain", new Dictionary<string, string>
        {
            ["aiBeta"] = AiBetaEnabled ? "true" : "false",
            ["cloud"] = AiAssistantService.IsCloudConfigured(_settings) ? "true" : "false",
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
            StatusMessage = "Sélectionnez un incident d'abord";
            return;
        }

        try
        {
            RecommendationFeedbackStore.Add(SelectedIncident.Item, useful);
            var msg = useful ? "Merci — marqué comme utile." : "Merci — on notera que ce n'était pas utile.";
            Detail.SetFeedbackMessage(msg);
            StatusMessage = msg;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Impossible d'enregistrer le feedback : {ex.Message}";
            Detail.SetFeedbackMessage(StatusMessage);
        }
    }

    [RelayCommand]
    private async Task FlushDnsAsync()
    {
        await RunMaintenanceAsync(
            "Vidage du cache DNS",
            () => SystemMaintenanceHelper.FlushDnsAsync(),
            "flush_dns");
    }

    [RelayCommand]
    private async Task ResetNetworkAsync()
    {
        await RunMaintenanceAsync(
            "Réinitialisation réseau",
            () => SystemMaintenanceHelper.ResetNetworkAsync(),
            "reset_network");
    }

    [RelayCommand]
    private async Task RunSfcAsync()
    {
        await RunMaintenanceAsync(
            "Vérification des fichiers système (SFC)",
            () => SystemMaintenanceHelper.RunSystemFileCheckerAsync(),
            "sfc_scan");
    }

    private async Task RunMaintenanceAsync(string label, Func<Task<CommandResult>> action, string telemetryName)
    {
        if (!ShowMaintenance)
        {
            return;
        }

        if (!IsAdmin)
        {
            MaintenanceMessage = "Les actions de maintenance nécessitent les droits administrateur.";
            HasMaintenanceMessage = true;
            return;
        }

        IsBusyMaintenance = true;
        MaintenanceMessage = $"{label} en cours… Cela peut prendre plusieurs minutes.";
        HasMaintenanceMessage = true;
        StatusMessage = MaintenanceMessage;

        try
        {
            var result = await action();
            MaintenanceMessage = result.GetUserSummary(label);
            HasMaintenanceMessage = true;
            StatusMessage = MaintenanceMessage;
            TelemetryService.Track(telemetryName, new Dictionary<string, string>
            {
                ["success"] = result.Success ? "true" : "false",
                ["exitCode"] = result.ExitCode.ToString()
            });
        }
        catch (Exception ex)
        {
            MaintenanceMessage = $"{label} a échoué : {ex.Message}";
            HasMaintenanceMessage = true;
            StatusMessage = MaintenanceMessage;
            TelemetryService.TrackException(telemetryName + "_failed", ex);
        }
        finally
        {
            IsBusyMaintenance = false;
        }
    }

    private void RefreshInsights()
    {
        var insights = _insightsService.Build(_allEvents);
        HealthHeadline = insights.HealthHeadline;
        ErrorCount = insights.CriticalCount;
        WarningCount = insights.WarningCount;
        RiskScore = insights.RiskScore;
        HealthSubtitle = insights.CriticalCount == 0 && insights.WarningCount == 0
            ? "Aucun problème récent détecté"
            : $"{insights.CriticalCount} erreur(s) · {insights.WarningCount} avertissement(s) · risque {insights.RiskScore}/100";

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
            ? $"Snapshot du {date:dd/MM/yyyy HH:mm}"
            : "Aucun point de comparaison";
    }

    private void RefreshToolsStatus()
    {
        var telemetry = TelemetryOptIn ? "ON" : "OFF";
        var ai = AiBetaEnabled ? "ON" : "OFF";
        ToolsStatus = $"Télémétrie {telemetry} · IA bêta {ai}";
        AiCloudStatus = AiAssistantService.IsCloudConfigured(_settings)
            ? "Cloud IA : configuré (clé détectée)"
            : "Cloud IA : non configuré (local uniquement)";
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

        var list = filtered.ToList();
        Incidents.Clear();
        foreach (var item in list)
        {
            Incidents.Add(new IncidentCardViewModel(item));
        }

        IsEmpty = Incidents.Count == 0;
        if (IsEmpty)
        {
            if (_comparisonMode && _allEvents.Count > 0)
            {
                EmptyTitle = "Aucun nouvel incident";
                EmptyMessage = "Rien de nouveau depuis votre dernier point de comparaison.";
            }
            else if (_allEvents.Count == 0)
            {
                EmptyTitle = "Aucun incident récent";
                EmptyMessage = "C'est une bonne nouvelle : aucune erreur ni avertissement dans ce journal.";
            }
            else
            {
                EmptyTitle = "Aucun résultat";
                EmptyMessage = "Essayez un autre mot-clé (ex. disque, réseau, mémoire).";
            }

            SelectedIncident = null;
            Detail.Clear();
        }
        else if (SelectedIncident == null || !Incidents.Any(i => ReferenceEquals(i.Item, SelectedIncident.Item)))
        {
            SelectedIncident = Incidents[0];
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

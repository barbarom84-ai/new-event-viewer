namespace EventViewer.Core;

public enum AutoFixKind
{
    None,
    FlushDns,
    ResetNetwork,
    RunSfc,
    DiskCleanup,
    DefenderQuickScan,
    OpenNetworkSettings,
    OpenStorageSettings,
    OpenWindowsUpdate,
    OpenReliabilityHistory,
    OpenDeviceManager
}

public sealed class AutoFixRecommendation
{
    public required AutoFixKind Kind { get; init; }
    public required string ButtonLabel { get; init; }
    public required string Title { get; init; }
    public required string Explanation { get; init; }
    public required string ConfirmMessage { get; init; }
    public bool RequiresAdmin { get; init; }
    public bool IsLongRunning { get; init; }
    public bool IsAvailableInStore { get; init; } = true;
    public bool CanExecuteAutomatically { get; init; } = true;
}

/// <summary>
/// Maps incidents to a safe one-click / two-click remediation when possible.
/// </summary>
public sealed class AutoFixService
{
    public AutoFixRecommendation Recommend(EventItem? item, bool isStoreBuild)
    {
        if (item == null)
        {
            return None(Loc.T("AutoFix.None.SelectTitle"), Loc.T("AutoFix.None.SelectExplanation"));
        }

        var haystack = $"{item.Tag?.Name} {item.Source} {item.ExplanationTitle} {item.Message}".ToLowerInvariant();
        var id = item.EventId;

        // Common benign DCOM (10016) — no network auto-fix.
        if (id == 10016 || TagDetector.IsDcom(item.Source, item.Message) || TagDetector.IsDcom(item.Source, item.ExplanationTitle))
        {
            return None(Loc.T("AutoFix.Dcom.Title"), Loc.T("AutoFix.Dcom.Explanation"));
        }

        AutoFixRecommendation Pick(AutoFixRecommendation preferred, AutoFixRecommendation storeSafe)
            => isStoreBuild && !preferred.IsAvailableInStore ? storeSafe : preferred;

        if (ContainsAny(haystack, "dns", "name resolution", "résolution") || id is 8003 or 1014)
        {
            return Pick(
                Build(AutoFixKind.FlushDns,
                    Loc.T("AutoFix.Dns.Button"), Loc.T("AutoFix.Dns.Title"),
                    Loc.T("AutoFix.Dns.Explanation"), Loc.T("AutoFix.Dns.Confirm"),
                    requiresAdmin: true, store: false),
                Build(AutoFixKind.OpenNetworkSettings,
                    Loc.T("AutoFix.NetworkSettings.Button"), Loc.T("AutoFix.NetworkSettings.Title"),
                    Loc.T("AutoFix.NetworkSettings.ExplanationStoreDns"), Loc.T("AutoFix.NetworkSettings.Confirm"),
                    requiresAdmin: false, store: true));
        }

        if (ContainsAny(haystack, "network", "réseau", "tcp", "winsock", "ethernet", "wifi", "connexion")
            || string.Equals(item.Tag?.Key, "Network", StringComparison.OrdinalIgnoreCase))
        {
            if (ContainsAny(haystack, "winsock", "tcp/ip", "adapter", "carte réseau"))
            {
                return Pick(
                    Build(AutoFixKind.ResetNetwork,
                        Loc.T("AutoFix.ResetNetwork.Button"), Loc.T("AutoFix.ResetNetwork.Title"),
                        Loc.T("AutoFix.ResetNetwork.Explanation"), Loc.T("AutoFix.ResetNetwork.Confirm"),
                        requiresAdmin: true, store: false),
                    Build(AutoFixKind.OpenNetworkSettings,
                        Loc.T("AutoFix.NetworkSettings.Button"), Loc.T("AutoFix.NetworkSettings.Title"),
                        Loc.T("AutoFix.NetworkSettings.ExplanationStoreReset"), Loc.T("AutoFix.NetworkSettings.Confirm"),
                        requiresAdmin: false, store: true));
            }

            return Pick(
                Build(AutoFixKind.FlushDns,
                    Loc.T("AutoFix.NetworkDns.Button"), Loc.T("AutoFix.NetworkDns.Title"),
                    Loc.T("AutoFix.NetworkDns.Explanation"), Loc.T("AutoFix.NetworkDns.Confirm"),
                    requiresAdmin: true, store: false),
                Build(AutoFixKind.OpenNetworkSettings,
                    Loc.T("AutoFix.NetworkSettings.Button"), Loc.T("AutoFix.NetworkDns.Title"),
                    Loc.T("AutoFix.NetworkSettings.ExplanationFirst"), Loc.T("AutoFix.NetworkSettings.Confirm"),
                    requiresAdmin: false, store: true));
        }

        if (ContainsAny(haystack, "disk", "disque", "espace", "volume", "ntfs", "storage")
            || id is 153 or 2019
            || string.Equals(item.Tag?.Key, "Hardware", StringComparison.OrdinalIgnoreCase))
        {
            if (ContainsAny(haystack, "espace", "space", "low") || id == 153)
            {
                return Build(AutoFixKind.DiskCleanup,
                    Loc.T("AutoFix.DiskCleanup.Button"), Loc.T("AutoFix.DiskCleanup.Title"),
                    Loc.T("AutoFix.DiskCleanup.Explanation"), Loc.T("AutoFix.DiskCleanup.Confirm"),
                    requiresAdmin: false, store: true);
            }

            return Build(AutoFixKind.OpenStorageSettings,
                Loc.T("AutoFix.Storage.Button"), Loc.T("AutoFix.Storage.Title"),
                Loc.T("AutoFix.Storage.Explanation"), Loc.T("AutoFix.Storage.Confirm"),
                requiresAdmin: false, store: true);
        }

        if (ContainsAny(haystack, "defender", "malware", "virus", "threat", "menace", "security")
            || string.Equals(item.Tag?.Key, "Security", StringComparison.OrdinalIgnoreCase)
            || id is 1116 or 1117)
        {
            return Pick(
                Build(AutoFixKind.DefenderQuickScan,
                    Loc.T("AutoFix.Defender.Button"), Loc.T("AutoFix.Defender.Title"),
                    Loc.T("AutoFix.Defender.Explanation"), Loc.T("AutoFix.Defender.Confirm"),
                    requiresAdmin: true, store: false),
                Build(AutoFixKind.OpenWindowsUpdate,
                    Loc.T("AutoFix.Update.Button"), Loc.T("AutoFix.Update.TitleCheck"),
                    Loc.T("AutoFix.Update.ExplanationStoreSecurity"), Loc.T("AutoFix.Update.Confirm"),
                    requiresAdmin: false, store: true));
        }

        if (ContainsAny(haystack, "update", "mise à jour", "windows update") || id is 19 or 20)
        {
            return Build(AutoFixKind.OpenWindowsUpdate,
                Loc.T("AutoFix.Update.Button"), Loc.T("AutoFix.Update.Title"),
                Loc.T("AutoFix.Update.Explanation"), Loc.T("AutoFix.Update.Confirm"),
                requiresAdmin: false, store: true);
        }

        // BSOD / bugcheck / unexpected shutdown — actionable first steps (not only dump tools).
        if (IsBugcheckLike(item, haystack))
        {
            return Build(
                AutoFixKind.OpenReliabilityHistory,
                Loc.T("AutoFix.Bugcheck.Button"),
                Loc.T("AutoFix.Bugcheck.Title"),
                Loc.T("AutoFix.Bugcheck.Explanation"),
                Loc.T("AutoFix.Bugcheck.Confirm"),
                requiresAdmin: false,
                store: true);
        }

        if (ContainsAny(haystack, "driver", "pilote", "device") || id == 219)
        {
            return Build(AutoFixKind.OpenDeviceManager,
                Loc.T("AutoFix.Drivers.Button"), Loc.T("AutoFix.Drivers.Title"),
                Loc.T("AutoFix.Drivers.Explanation"), Loc.T("AutoFix.Drivers.Confirm"),
                requiresAdmin: false, store: true);
        }

        if (ContainsAny(haystack, "service", "corrupt", "fichier système", "integrity")
            || string.Equals(item.Tag?.Key, "Service", StringComparison.OrdinalIgnoreCase)
            || id is 7000 or 7001 or 7031 or 7034)
        {
            return Pick(
                Build(AutoFixKind.RunSfc,
                    Loc.T("AutoFix.Sfc.Button"), Loc.T("AutoFix.Sfc.Title"),
                    Loc.T("AutoFix.Sfc.Explanation"), Loc.T("AutoFix.Sfc.Confirm"),
                    requiresAdmin: true, store: false, longRunning: true),
                Build(AutoFixKind.OpenWindowsUpdate,
                    Loc.T("AutoFix.Update.Title"), Loc.T("AutoFix.Update.TitleRecommended"),
                    Loc.T("AutoFix.Update.ExplanationStoreSfc"), Loc.T("AutoFix.Update.Confirm"),
                    requiresAdmin: false, store: true));
        }

        if (ContainsAny(haystack, "memory", "mémoire", "ram", "out of memory")
            || string.Equals(item.Tag?.Name, "MÉMOIRE", StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Tag?.Key, "Memory", StringComparison.OrdinalIgnoreCase))
        {
            return None(Loc.T("AutoFix.Memory.Title"), Loc.T("AutoFix.Memory.Explanation"));
        }

        if (isStoreBuild)
        {
            return Build(AutoFixKind.OpenWindowsUpdate,
                Loc.T("AutoFix.Update.Title"), Loc.T("AutoFix.Update.TitleRecommended"),
                Loc.T("AutoFix.Update.ExplanationFallback"), Loc.T("AutoFix.Update.Confirm"),
                requiresAdmin: false, store: true);
        }

        return None(Loc.T("AutoFix.Manual.Title"), Loc.T("AutoFix.Manual.Explanation"));
    }

    public async Task<CommandResult> ExecuteAsync(AutoFixKind kind)
    {
        return kind switch
        {
            AutoFixKind.FlushDns => await SystemMaintenanceHelper.FlushDnsAsync(),
            AutoFixKind.ResetNetwork => await SystemMaintenanceHelper.ResetNetworkAsync(),
            AutoFixKind.RunSfc => await SystemMaintenanceHelper.RunSystemFileCheckerAsync(),
            AutoFixKind.DiskCleanup => await SystemMaintenanceHelper.OpenDiskCleanupAsync(),
            AutoFixKind.DefenderQuickScan => await SystemMaintenanceHelper.RunDefenderQuickScanAsync(),
            AutoFixKind.OpenNetworkSettings => await SystemMaintenanceHelper.OpenUriAsync("ms-settings:network"),
            AutoFixKind.OpenStorageSettings => await SystemMaintenanceHelper.OpenUriAsync("ms-settings:storagesense"),
            AutoFixKind.OpenWindowsUpdate => await SystemMaintenanceHelper.OpenUriAsync("ms-settings:windowsupdate"),
            AutoFixKind.OpenReliabilityHistory => await SystemMaintenanceHelper.OpenReliabilityHistoryAsync(),
            AutoFixKind.OpenDeviceManager => await SystemMaintenanceHelper.OpenDeviceManagerAsync(),
            _ => new CommandResult
            {
                Success = false,
                ExitCode = -1,
                Error = Loc.T("AutoFix.Execute.None")
            }
        };
    }

    private static bool IsBugcheckLike(EventItem item, string haystack)
    {
        if (item.EventId is 1001 or 41 or 6008)
        {
            return true;
        }

        return ContainsAny(haystack,
            "bugcheck", "bsod", "blue screen", "écran bleu", "ecran bleu",
            "systemerrorreporting", "unexpected shutdown", "arrêt critique", "arret critique");
    }

    private static AutoFixRecommendation Build(
        AutoFixKind kind,
        string button,
        string title,
        string explanation,
        string confirm,
        bool requiresAdmin,
        bool store,
        bool longRunning = false,
        bool canExecute = true)
        => new()
        {
            Kind = kind,
            ButtonLabel = button,
            Title = title,
            Explanation = explanation,
            ConfirmMessage = confirm,
            RequiresAdmin = requiresAdmin,
            IsLongRunning = longRunning,
            IsAvailableInStore = store,
            CanExecuteAutomatically = canExecute
        };

    private static AutoFixRecommendation None(string title, string explanation)
        => new()
        {
            Kind = AutoFixKind.None,
            ButtonLabel = Loc.T("AutoFix.Button.None"),
            Title = title,
            Explanation = explanation,
            ConfirmMessage = string.Empty,
            RequiresAdmin = false,
            IsAvailableInStore = true,
            CanExecuteAutomatically = false
        };

    private static bool ContainsAny(string haystack, params string[] needles)
        => needles.Any(n => haystack.Contains(n, StringComparison.OrdinalIgnoreCase));
}

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
    OpenWindowsUpdate
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
            return None("Sélectionnez un incident", "Choisissez une ligne pour proposer une correction.");
        }

        var haystack = $"{item.Tag?.Name} {item.Source} {item.ExplanationTitle} {item.Message} {item.FullMessage}".ToLowerInvariant();
        var id = item.EventId;

        AutoFixRecommendation Pick(AutoFixRecommendation preferred, AutoFixRecommendation storeSafe)
            => isStoreBuild && !preferred.IsAvailableInStore ? storeSafe : preferred;

        if (ContainsAny(haystack, "dns", "name resolution", "résolution") || id is 8003 or 1014)
        {
            return Pick(
                Build(AutoFixKind.FlushDns, "Corriger le DNS", "Vider le cache DNS",
                    "Souvent suffisant quand la navigation ou la résolution de noms plante.",
                    "On va vider le cache DNS de Windows. Action rapide et sans danger.",
                    requiresAdmin: true, store: false),
                Build(AutoFixKind.OpenNetworkSettings, "Ouvrir Réglages réseau", "Vérifier le réseau",
                    "Ouvre les paramètres réseau Windows. Pour vider le DNS automatiquement, utilisez la version USB.",
                    "Ouvrir les paramètres réseau ?", requiresAdmin: false, store: true));
        }

        if (ContainsAny(haystack, "network", "réseau", "tcp", "winsock", "ethernet", "wifi", "connexion")
            || string.Equals(item.Tag?.Name, "RÉSEAU", StringComparison.OrdinalIgnoreCase))
        {
            if (ContainsAny(haystack, "winsock", "tcp/ip", "adapter", "carte réseau"))
            {
                return Pick(
                    Build(AutoFixKind.ResetNetwork, "Réparer le réseau", "Réinitialiser la pile réseau",
                        "Remet Windows réseau à zéro (Winsock + TCP/IP). Un redémarrage peut être demandé ensuite.",
                        "Réinitialiser le réseau maintenant ? Cela peut couper Internet brièvement. Un redémarrage peut être nécessaire.",
                        requiresAdmin: true, store: false),
                    Build(AutoFixKind.OpenNetworkSettings, "Ouvrir Réglages réseau", "Vérifier le réseau",
                        "Ouvre les paramètres réseau. La réinitialisation complète est disponible en version USB.",
                        "Ouvrir les paramètres réseau ?", requiresAdmin: false, store: true));
            }

            return Pick(
                Build(AutoFixKind.FlushDns, "Corriger le réseau (DNS)", "Premier geste réseau",
                    "On commence par vider le cache DNS, la correction la plus sûre.",
                    "Vider le cache DNS maintenant ?", requiresAdmin: true, store: false),
                Build(AutoFixKind.OpenNetworkSettings, "Ouvrir Réglages réseau", "Premier geste réseau",
                    "Ouvre les paramètres réseau pour vérifier connexion / Wi‑Fi / résolution.",
                    "Ouvrir les paramètres réseau ?", requiresAdmin: false, store: true));
        }

        if (ContainsAny(haystack, "disk", "disque", "espace", "volume", "ntfs", "storage")
            || id is 153 or 2019
            || string.Equals(item.Tag?.Name, "MATÉRIEL", StringComparison.OrdinalIgnoreCase))
        {
            if (ContainsAny(haystack, "espace", "space", "low") || id == 153)
            {
                return Build(AutoFixKind.DiskCleanup, "Libérer de l'espace", "Nettoyage de disque",
                    "Ouvre l'outil Windows de nettoyage pour récupérer de l'espace.",
                    "Ouvrir le Nettoyage de disque Windows ?", requiresAdmin: false, store: true);
            }

            return Build(AutoFixKind.OpenStorageSettings, "Ouvrir Stockage", "Vérifier l'espace disque",
                "Ouvre les paramètres Stockage Windows pour voir ce qui prend de la place.",
                "Ouvrir les paramètres de stockage ?", requiresAdmin: false, store: true);
        }

        if (ContainsAny(haystack, "defender", "malware", "virus", "threat", "menace", "security")
            || string.Equals(item.Tag?.Name, "SÉCURITÉ", StringComparison.OrdinalIgnoreCase)
            || id is 1116 or 1117)
        {
            return Pick(
                Build(AutoFixKind.DefenderQuickScan, "Analyser avec Defender", "Analyse rapide Windows Defender",
                    "Lance une analyse rapide pour vérifier s'il reste une menace.",
                    "Lancer une analyse rapide Windows Defender ?", requiresAdmin: true, store: false),
                Build(AutoFixKind.OpenWindowsUpdate, "Ouvrir Windows Update", "Vérifier les protections",
                    "Dans la version Store, ouvrez Windows Update / Sécurité Windows manuellement.",
                    "Ouvrir Windows Update ?", requiresAdmin: false, store: true));
        }

        if (ContainsAny(haystack, "update", "mise à jour", "windows update") || id is 19 or 20)
        {
            return Build(AutoFixKind.OpenWindowsUpdate, "Ouvrir Windows Update", "Vérifier les mises à jour",
                "Ouvre Windows Update pour réessayer l'installation.",
                "Ouvrir Windows Update ?", requiresAdmin: false, store: true);
        }

        if (ContainsAny(haystack, "service", "corrupt", "fichier système", "integrity")
            || string.Equals(item.Tag?.Name, "SERVICE", StringComparison.OrdinalIgnoreCase)
            || id is 7000 or 7001 or 7031 or 7034 or 41 or 6008)
        {
            return Pick(
                Build(AutoFixKind.RunSfc, "Réparer Windows (SFC)", "Vérification des fichiers système",
                    "SFC cherche et répare les fichiers Windows corrompus. Cela peut prendre 10 à 30 minutes.",
                    "Lancer SFC /scannow ? Laissez l'ordinateur allumé, cela peut prendre longtemps.",
                    requiresAdmin: true, store: false, longRunning: true),
                Build(AutoFixKind.OpenWindowsUpdate, "Vérifier les mises à jour", "Geste recommandé",
                    "La réparation SFC n'est pas disponible dans le Store. Mettez Windows à jour, puis redémarrez.",
                    "Ouvrir Windows Update ?", requiresAdmin: false, store: true));
        }

        if (ContainsAny(haystack, "memory", "mémoire", "ram", "out of memory")
            || string.Equals(item.Tag?.Name, "MÉMOIRE", StringComparison.OrdinalIgnoreCase))
        {
            return None(
                "Fermez des applications puis redémarrez",
                "Pour la mémoire, le geste le plus sûr est de fermer les apps lourdes puis redémarrer le PC.");
        }

        if (isStoreBuild)
        {
            return Build(AutoFixKind.OpenWindowsUpdate, "Vérifier les mises à jour", "Geste recommandé",
                "Pour cet incident, ouvrez Windows Update puis redémarrez si le problème continue.",
                "Ouvrir Windows Update ?", requiresAdmin: false, store: true);
        }

        return None(
            "Correction manuelle",
            "Pour cet incident, suivez le conseil « Que faire ». Aucune action automatique sûre n'est disponible.");
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
            _ => new CommandResult
            {
                Success = false,
                ExitCode = -1,
                Error = "Aucune action automatique pour cet incident."
            }
        };
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
            ButtonLabel = "Pas d'auto-correction",
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

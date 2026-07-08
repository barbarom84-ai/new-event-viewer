namespace EventViewer.Core;

/// <summary>
/// Detects category tags for system events (no UI dependencies).
/// </summary>
public sealed class TagDetector
{
    private static readonly Dictionary<string, TagInfo> TagCategories = new()
    {
        ["MATÉRIEL"] = new TagInfo
        {
            Name = "MATÉRIEL",
            Keywords = ["disk", "bad block", "ntfs", "hardware", "drive", "ssd", "hdd", "sata", "storage", "volume", "partition"],
            ColorHex = "#FFF44336",
            Advice = "Problème matériel possible. Vérifiez l'état du disque, sauvegardez vos données importantes, puis exécutez une vérification du disque si besoin."
        },
        ["RÉSEAU"] = new TagInfo
        {
            Name = "RÉSEAU",
            Keywords = ["network", "dns", "tcp", "ip", "ethernet", "wifi", "connection", "internet", "lan", "wan", "dhcp", "router"],
            ColorHex = "#FF2196F3",
            Advice = "Problème réseau possible. Vérifiez votre connexion, redémarrez la box si besoin, puis testez un autre serveur DNS."
        },
        ["MÉMOIRE"] = new TagInfo
        {
            Name = "MÉMOIRE",
            Keywords = ["memory", "ram", "page file", "paging", "swap", "virtual memory", "out of memory", "oom"],
            ColorHex = "#FF9C27B0",
            Advice = "Problème de mémoire possible. Fermez les applications inutilisées et redémarrez l'ordinateur si nécessaire."
        },
        ["SERVICE"] = new TagInfo
        {
            Name = "SERVICE",
            Keywords = ["service", "svchost", "daemon", "timeout", "failed to start", "stopped unexpectedly"],
            ColorHex = "#FFFF9800",
            Advice = "Un service Windows a rencontré un problème. Un redémarrage peut aider ; sinon consultez les détails de l'événement."
        },
        ["SÉCURITÉ"] = new TagInfo
        {
            Name = "SÉCURITÉ",
            Keywords = ["security", "virus", "malware", "threat", "firewall", "authentication", "login", "credential", "unauthorized"],
            ColorHex = "#FFFFC107",
            Advice = "Alerte liée à la sécurité. Lancez une analyse Windows Defender et vérifiez les connexions récentes si besoin."
        }
    };

    public TagInfo? DetectTag(string message, string source)
    {
        var textToAnalyze = $"{message} {source}".ToLowerInvariant();

        foreach (var category in TagCategories.Values)
        {
            if (category.Keywords.Any(keyword => textToAnalyze.Contains(keyword)))
            {
                return category;
            }
        }

        return null;
    }

    public IEnumerable<TagInfo> GetAllTags() => TagCategories.Values;
}

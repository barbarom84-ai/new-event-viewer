using System.Text.RegularExpressions;

namespace EventViewer.Core;

/// <summary>
/// Detects category tags for system events (no UI dependencies).
/// </summary>
public sealed class TagDetector
{
    private static readonly (string Key, string[] Keywords, string ColorHex)[] Categories =
    [
        ("Hardware", ["disk", "bad block", "ntfs", "hardware", "drive", "ssd", "hdd", "sata", "storage", "volume", "partition"], "#FFF44336"),
        ("Network", ["network", "dns", "tcp", "ethernet", "wifi", "wi-fi", "internet", "dhcp", "router", "connexion", "connection"], "#FF2196F3"),
        ("Memory", ["memory", "ram", "page file", "paging", "swap", "virtual memory", "out of memory", "oom"], "#FF9C27B0"),
        ("Service", ["service", "svchost", "daemon", "timeout", "failed to start", "stopped unexpectedly"], "#FFFF9800"),
        ("Security", ["security", "virus", "malware", "threat", "firewall", "authentication", "login", "credential", "unauthorized"], "#FFFFC107")
    ];

    public TagInfo? DetectTag(string message, string source)
    {
        if (IsDcom(source, message))
        {
            return null;
        }

        var textToAnalyze = $"{message} {source}".ToLowerInvariant();

        foreach (var (key, keywords, color) in Categories)
        {
            if (keywords.Any(keyword => ContainsKeyword(textToAnalyze, keyword)))
            {
                return new TagInfo
                {
                    Key = key,
                    Name = Loc.T($"Tag.{key}"),
                    Keywords = keywords,
                    ColorHex = color,
                    Advice = Loc.T($"Tag.{key}.Advice")
                };
            }
        }

        return null;
    }

    public IEnumerable<TagInfo> GetAllTags()
        => Categories.Select(c => new TagInfo
        {
            Key = c.Key,
            Name = Loc.T($"Tag.{c.Key}"),
            Keywords = c.Keywords,
            ColorHex = c.ColorHex,
            Advice = Loc.T($"Tag.{c.Key}.Advice")
        });

    public static bool IsDcom(string? source, string? message)
    {
        var blob = $"{source} {message}".ToLowerInvariant();
        return blob.Contains("dcom", StringComparison.Ordinal)
               || blob.Contains("distributedcom", StringComparison.Ordinal)
               || blob.Contains("distributed com", StringComparison.Ordinal);
    }

    private static bool ContainsKeyword(string text, string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return false;
        }

        if (keyword.Length <= 3)
        {
            return Regex.IsMatch(text, $@"\b{Regex.Escape(keyword)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return text.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }
}

using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace EventViewer.Core;

/// <summary>
/// Resolves DCOM/COM CLSID and AppID GUIDs to human-readable Windows component names.
/// </summary>
public static partial class ComComponentResolver
{
    private static readonly Dictionary<string, string> KnownFriendlyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WaaSMedicSvc"] = "Windows Update Medic",
        ["RuntimeBroker"] = "Runtime Broker",
        ["ShellServiceHost"] = "Shell Service Host",
        ["PerAppRuntimeBroker"] = "Runtime Broker (par application)",
        ["ImmersiveShell"] = "Shell immersif Windows",
        ["WpnUserService"] = "Notifications push Windows",
        ["WpnService"] = "Service de notifications Windows",
        ["Schedule"] = "Planificateur de tâches",
        ["System"] = "Système Windows"
    };

    [GeneratedRegex(@"\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}", RegexOptions.CultureInvariant)]
    private static partial Regex GuidRegex();

    public static ComComponentInfo? ResolveFromMessage(string? message, string? source = null)
    {
        if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var guids = ExtractGuids(message ?? string.Empty)
            .Where(g => !IsNullGuid(g))
            .ToList();
        if (guids.Count == 0)
        {
            return null;
        }

        // DCOM 10016 usually has CLSID then AppID. Prefer AppID (application identity).
        IEnumerable<string> order = guids.Count >= 2
            ? new[] { guids[1], guids[0] }.Concat(guids.Skip(2))
            : guids;

        ComComponentInfo? bestUnresolved = null;
        foreach (var guid in order)
        {
            var resolved = ResolveGuid(guid);
            if (resolved == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(resolved.RegistryName) ||
                !string.IsNullOrWhiteSpace(resolved.FriendlyName))
            {
                return resolved;
            }

            bestUnresolved ??= resolved;
        }

        return bestUnresolved ?? new ComComponentInfo
        {
            Guid = guids[0],
            RegistryName = null,
            FriendlyName = null
        };
    }

    public static ComComponentInfo? ResolveGuid(string guid)
    {
        var normalized = NormalizeGuid(guid);
        if (normalized == null)
        {
            return null;
        }

        var registryName =
            TryReadDefault(Registry.ClassesRoot, $@"AppID\{normalized}") ??
            TryReadDefault(Registry.ClassesRoot, $@"CLSID\{normalized}") ??
            TryReadValue(Registry.ClassesRoot, $@"AppID\{normalized}", "LocalService") ??
            TryReadDefault(Registry.LocalMachine, $@"SOFTWARE\Classes\AppID\{normalized}") ??
            TryReadDefault(Registry.LocalMachine, $@"SOFTWARE\Classes\CLSID\{normalized}") ??
            TryReadValue(Registry.LocalMachine, $@"SOFTWARE\Classes\AppID\{normalized}", "LocalService");

        string? friendly = null;
        if (!string.IsNullOrWhiteSpace(registryName) &&
            KnownFriendlyNames.TryGetValue(registryName.Trim(), out var mapped))
        {
            friendly = mapped;
        }

        return new ComComponentInfo
        {
            Guid = normalized,
            RegistryName = string.IsNullOrWhiteSpace(registryName) ? null : registryName.Trim(),
            FriendlyName = friendly
        };
    }

    public static IReadOnlyList<string> ExtractGuids(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return GuidRegex().Matches(text)
            .Select(m => NormalizeGuid(m.Value)!)
            .Where(g => g != null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }

    private static string? NormalizeGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith('{'))
        {
            trimmed = "{" + trimmed.Trim('{', '}') + "}";
        }

        return Guid.TryParse(trimmed.Trim('{', '}'), out var g)
            ? g.ToString("B").ToUpperInvariant()
            : null;
    }

    private static bool IsNullGuid(string guid)
        => string.Equals(guid, "{00000000-0000-0000-0000-000000000000}", StringComparison.OrdinalIgnoreCase);

    private static string? TryReadDefault(RegistryKey root, string subKeyPath)
    {
        try
        {
            using var key = root.OpenSubKey(subKeyPath);
            return key?.GetValue(null)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadValue(RegistryKey root, string subKeyPath, string valueName)
    {
        try
        {
            using var key = root.OpenSubKey(subKeyPath);
            return key?.GetValue(valueName)?.ToString();
        }
        catch
        {
            return null;
        }
    }
}

public sealed class ComComponentInfo
{
    public required string Guid { get; init; }
    public string? RegistryName { get; init; }
    public string? FriendlyName { get; init; }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(FriendlyName) && !string.IsNullOrWhiteSpace(RegistryName))
            {
                return $"{FriendlyName} ({RegistryName})";
            }

            if (!string.IsNullOrWhiteSpace(FriendlyName))
            {
                return FriendlyName;
            }

            if (!string.IsNullOrWhiteSpace(RegistryName))
            {
                return RegistryName;
            }

            return "Composant COM inconnu";
        }
    }

    /// <summary>Shows both the readable name and the GUID.</summary>
    public string DisplayBoth => $"{DisplayName} · {Guid}";
}

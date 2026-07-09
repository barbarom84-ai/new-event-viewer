using System.Globalization;
using System.Text.Json;

namespace EventViewer.Core;

/// <summary>
/// Supported UI languages for Store distribution.
/// </summary>
public static class AppLanguage
{
    public const string System = "system";
    public const string French = "fr";
    public const string English = "en";
    public const string Italian = "it";

    public static readonly string[] Supported = [French, English, Italian];

    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Equals(System, StringComparison.OrdinalIgnoreCase))
        {
            return System;
        }

        var two = code.Trim().Replace('_', '-');
        if (two.StartsWith("fr", StringComparison.OrdinalIgnoreCase)) return French;
        if (two.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return English;
        if (two.StartsWith("it", StringComparison.OrdinalIgnoreCase)) return Italian;
        return System;
    }

    public static string ResolveEffective(string? preference)
    {
        var pref = Normalize(preference);
        if (pref != System)
        {
            return pref;
        }

        var ui = CultureInfo.CurrentUICulture.Name;
        var normalized = Normalize(ui);
        return normalized == System ? French : normalized;
    }

    public static CultureInfo ToCulture(string effectiveCode) => effectiveCode switch
    {
        English => CultureInfo.GetCultureInfo("en-US"),
        Italian => CultureInfo.GetCultureInfo("it-IT"),
        _ => CultureInfo.GetCultureInfo("fr-FR")
    };

    public static string DisplayName(string code) => code switch
    {
        System => Loc.T("Lang.System"),
        French => "Français",
        English => "English",
        Italian => "Italiano",
        _ => code
    };
}

/// <summary>
/// Shared localization for Core + WinUI. JSON catalogs embedded as resources.
/// </summary>
public static class Loc
{
    private static readonly object Sync = new();
    private static Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, string> _fallback = new(StringComparer.OrdinalIgnoreCase);
    private static string _effective = AppLanguage.French;
    private static bool _initialized;

    public static string EffectiveLanguage => _effective;

    public static event Action? LanguageChanged;

    public static void Initialize(string? preference = AppLanguage.System)
    {
        Apply(preference);
    }

    public static void Apply(string? preference)
    {
        lock (Sync)
        {
            EnsureFallbackLoaded();
            _effective = AppLanguage.ResolveEffective(preference);
            _strings = LoadCatalog(_effective);
            var culture = AppLanguage.ToCulture(_effective);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            _initialized = true;
        }

        LanguageChanged?.Invoke();
    }

    public static string T(string key)
    {
        EnsureInitialized();
        if (_strings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (_fallback.TryGetValue(key, out var fr) && !string.IsNullOrEmpty(fr))
        {
            return fr;
        }

        return key;
    }

    public static string T(string key, params object[] args)
    {
        try
        {
            return string.Format(CultureInfo.CurrentUICulture, T(key), args);
        }
        catch (FormatException)
        {
            return T(key);
        }
    }

    public static bool Has(string key)
    {
        EnsureInitialized();
        return _strings.ContainsKey(key) || _fallback.ContainsKey(key);
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        Initialize(AppLanguage.System);
    }

    private static void EnsureFallbackLoaded()
    {
        if (_fallback.Count == 0)
        {
            _fallback = LoadCatalog(AppLanguage.French);
        }
    }

    private static Dictionary<string, string> LoadCatalog(string lang)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var json = TryReadCatalogJson(lang);
            if (string.IsNullOrWhiteSpace(json))
            {
                return map;
            }

            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    map[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }
        }
        catch
        {
            // Keep empty map; callers fall back to keys / FR.
        }

        return map;
    }

    private static string? TryReadCatalogJson(string lang)
    {
        var fileName = $"strings.{lang}.json";

        // 1) Next to the running assembly / test output.
        foreach (var root in new[]
                 {
                     AppContext.BaseDirectory,
                     Path.GetDirectoryName(typeof(Loc).Assembly.Location) ?? string.Empty
                 })
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var candidates = new[]
            {
                Path.Combine(root, "Localization", fileName),
                Path.Combine(root, fileName)
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
            }
        }

        // 2) Embedded resource fallback.
        var asm = typeof(Loc).Assembly;
        var expected = $"EventViewer.Core.strings.{lang}.json";
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.Equals(expected, StringComparison.OrdinalIgnoreCase))
            ?? asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName == null)
        {
            return null;
        }

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

/// <summary>
/// Invariant event severity used for logic (not display).
/// </summary>
public enum EventSeverityKind
{
    Information,
    Warning,
    Error
}

public static class EventSeverity
{
    public const string Error = "Error";
    public const string Warning = "Warning";
    public const string Information = "Information";

    public static string FromEntryType(System.Diagnostics.EventLogEntryType entryType) => entryType switch
    {
        System.Diagnostics.EventLogEntryType.Error => Error,
        System.Diagnostics.EventLogEntryType.Warning => Warning,
        _ => Information
    };

    public static string Display(string? kind) => kind switch
    {
        Error => Loc.T("Level.Error"),
        Warning => Loc.T("Level.Warning"),
        _ => Loc.T("Level.Information")
    };

    public static bool IsError(string? kind)
        => string.Equals(kind, Error, StringComparison.OrdinalIgnoreCase)
           || string.Equals(kind, "Erreur", StringComparison.OrdinalIgnoreCase);

    public static bool IsWarning(string? kind)
        => string.Equals(kind, Warning, StringComparison.OrdinalIgnoreCase)
           || string.Equals(kind, "Avertissement", StringComparison.OrdinalIgnoreCase);
}

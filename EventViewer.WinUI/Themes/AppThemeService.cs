using EventViewer.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace EventViewer.WinUI.Themes;

public sealed class ThemeOption
{
    public required string Id { get; init; }
    public required string DisplayName { get; set; }
}

/// <summary>
/// Applies app color themes by mutating the shared App* SolidColorBrush instances.
/// </summary>
public static class AppThemeService
{
    public const string Dark = "dark";
    public const string Light = "light";
    public const string Midnight = "midnight";
    public const string Forest = "forest";

    public static readonly string[] Supported = [Dark, Light, Midnight, Forest];

    public static string CurrentThemeId { get; private set; } = Dark;

    public static ElementTheme CurrentElementTheme { get; private set; } = ElementTheme.Dark;

    public static string Normalize(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Dark;
        }

        var value = id.Trim().ToLowerInvariant();
        return Supported.Contains(value) ? value : Dark;
    }

    public static string DisplayName(string id) => Normalize(id) switch
    {
        Light => Loc.T("Theme.Light"),
        Midnight => Loc.T("Theme.Midnight"),
        Forest => Loc.T("Theme.Forest"),
        _ => Loc.T("Theme.Dark")
    };

    public static IReadOnlyList<ThemeOption> CreateOptions()
        => Supported.Select(id => new ThemeOption { Id = id, DisplayName = DisplayName(id) }).ToList();

    public static void Apply(string? themeId, FrameworkElement? root = null)
    {
        var id = Normalize(themeId);
        var palette = Resolve(id);

        // Application.Resources is not safe until after App construction completes.
        var app = Application.Current;
        if (app == null)
        {
            return;
        }

        ResourceDictionary resources;
        try
        {
            resources = app.Resources;
        }
        catch (Exception)
        {
            return;
        }

        // Only mutate our own App* brushes. Do not replace WinUI system theme keys.
        SetBrushColor(resources, "AppBackgroundBrush", palette.Background);
        SetBrushColor(resources, "AppSurfaceBrush", palette.Surface);
        SetBrushColor(resources, "AppSurfaceElevatedBrush", palette.SurfaceElevated);
        SetBrushColor(resources, "AppBorderBrush", palette.Border);
        SetBrushColor(resources, "AppTextBrush", palette.Text);
        SetBrushColor(resources, "AppMutedBrush", palette.Muted);
        SetBrushColor(resources, "AppAccentBrush", palette.Accent);
        SetBrushColor(resources, "AppCriticalBrush", palette.Critical);
        SetBrushColor(resources, "AppWarningBrush", palette.Warning);
        SetBrushColor(resources, "AppInfoBrush", palette.Info);
        SetBrushColor(resources, "AppActionBrush", palette.Action);

        CurrentThemeId = id;
        CurrentElementTheme = palette.IsLight ? ElementTheme.Light : ElementTheme.Dark;

        ApplyElementTheme(root);
        if (App.MainWindow?.Content is FrameworkElement windowRoot && !ReferenceEquals(windowRoot, root))
        {
            ApplyElementTheme(windowRoot);
        }
    }

    /// <summary>
    /// ContentDialog / flyouts do not always inherit the page theme — set explicitly.
    /// </summary>
    public static void ApplyElementTheme(FrameworkElement? element)
    {
        if (element == null)
        {
            return;
        }

        element.RequestedTheme = CurrentElementTheme;
    }

    public static void StyleContentDialog(ContentDialog dialog)
    {
        ApplyElementTheme(dialog);
        dialog.Background = Brush("AppSurfaceBrush");
        dialog.Foreground = Brush("AppTextBrush");
        dialog.BorderBrush = Brush("AppBorderBrush");
        dialog.CornerRadius = new CornerRadius(12);

        // WinUI light resources can wash out dialog chrome — pin our brushes.
        dialog.Resources["ContentDialogBackground"] = Brush("AppSurfaceBrush");
        dialog.Resources["ContentDialogForeground"] = Brush("AppTextBrush");
        dialog.Resources["ContentDialogBorderBrush"] = Brush("AppBorderBrush");
        dialog.Resources["TextFillColorPrimaryBrush"] = Brush("AppTextBrush");
        dialog.Resources["TextFillColorSecondaryBrush"] = Brush("AppMutedBrush");
        dialog.Resources["ControlFillColorDefaultBrush"] = Brush("AppSurfaceElevatedBrush");
        dialog.Resources["ControlStrokeColorDefaultBrush"] = Brush("AppBorderBrush");
    }

    private static Brush Brush(string key)
    {
        if (Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(ColorFromHex("#FF1A1D24"));
    }

    private static void SetBrushColor(ResourceDictionary resources, string key, Color color)
    {
        if (resources.TryGetValue(key, out var existing) && existing is SolidColorBrush brush)
        {
            brush.Color = color;
            return;
        }

        resources[key] = new SolidColorBrush(color);
    }

    private static ThemePalette Resolve(string id) => id switch
    {
        // Soft paper light theme: avoid pure white glare, keep secondary text readable.
        Light => new ThemePalette(
            IsLight: true,
            Background: ColorFromHex("#FFE4E8EE"),
            Surface: ColorFromHex("#FFF3F5F8"),
            SurfaceElevated: ColorFromHex("#FFE8ECF2"),
            Border: ColorFromHex("#FFA8B2C0"),
            Text: ColorFromHex("#FF12151A"),
            Muted: ColorFromHex("#FF3D4654"),
            Accent: ColorFromHex("#FF0B7F76"),
            Critical: ColorFromHex("#FFB42318"),
            Warning: ColorFromHex("#FF8A5A00"),
            Info: ColorFromHex("#FF0B5CAD"),
            Action: ColorFromHex("#FF0B5CAD")),
        Midnight => new ThemePalette(
            IsLight: false,
            Background: ColorFromHex("#FF0B1220"),
            Surface: ColorFromHex("#FF121A2B"),
            SurfaceElevated: ColorFromHex("#FF1A2438"),
            Border: ColorFromHex("#FF2A3650"),
            Text: ColorFromHex("#FFE7ECF5"),
            Muted: ColorFromHex("#FF9AA8C0"),
            Accent: ColorFromHex("#FF5B8CFF"),
            Critical: ColorFromHex("#FFFF6B81"),
            Warning: ColorFromHex("#FFFFC857"),
            Info: ColorFromHex("#FF7DD3FC"),
            Action: ColorFromHex("#FF3B82F6")),
        Forest => new ThemePalette(
            IsLight: false,
            Background: ColorFromHex("#FF0F1410"),
            Surface: ColorFromHex("#FF171E18"),
            SurfaceElevated: ColorFromHex("#FF1F2920"),
            Border: ColorFromHex("#FF2E3B30"),
            Text: ColorFromHex("#FFE8F0E9"),
            Muted: ColorFromHex("#FF9BB0A0"),
            Accent: ColorFromHex("#FF3DDC97"),
            Critical: ColorFromHex("#FFFF6B6B"),
            Warning: ColorFromHex("#FFFFC857"),
            Info: ColorFromHex("#FF6CB6FF"),
            Action: ColorFromHex("#FF2F6FED")),
        _ => new ThemePalette(
            IsLight: false,
            Background: ColorFromHex("#FF111318"),
            Surface: ColorFromHex("#FF1A1D24"),
            SurfaceElevated: ColorFromHex("#FF22262F"),
            Border: ColorFromHex("#FF2E3440"),
            Text: ColorFromHex("#FFE8EAED"),
            Muted: ColorFromHex("#FF9AA0A6"),
            Accent: ColorFromHex("#FF3DDC97"),
            Critical: ColorFromHex("#FFFF6B6B"),
            Warning: ColorFromHex("#FFFFC857"),
            Info: ColorFromHex("#FF6CB6FF"),
            Action: ColorFromHex("#FF1F6FEB"))
    };

    private static Color ColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            hex = "FF" + hex;
        }

        var value = Convert.ToUInt32(hex, 16);
        return Color.FromArgb(
            (byte)((value >> 24) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF));
    }

    private readonly record struct ThemePalette(
        bool IsLight,
        Color Background,
        Color Surface,
        Color SurfaceElevated,
        Color Border,
        Color Text,
        Color Muted,
        Color Accent,
        Color Critical,
        Color Warning,
        Color Info,
        Color Action);
}

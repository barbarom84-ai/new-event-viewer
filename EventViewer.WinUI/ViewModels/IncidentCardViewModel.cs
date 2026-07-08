using CommunityToolkit.Mvvm.ComponentModel;
using EventViewer.Core;

namespace EventViewer.WinUI.ViewModels;

public partial class IncidentCardViewModel : ObservableObject
{
    public IncidentCardViewModel(EventItem item)
    {
        Item = item;
        Title = string.IsNullOrWhiteSpace(item.ExplanationTitle)
            ? Truncate(item.Message, 80)
            : item.ExplanationTitle;
        SeverityLabel = HealthCopy.NoviceSeverity(item.Level);
        RelativeTime = RelativeTimeFormatter.Format(item.TimeCreatedAt);
        SecondaryInfo = string.IsNullOrWhiteSpace(item.Tag?.Name)
            ? item.Source
            : $"{item.Tag!.Name} · {item.Source}";
        IsCritical = string.Equals(item.Level, "Erreur", StringComparison.OrdinalIgnoreCase);
        IsWarning = string.Equals(item.Level, "Avertissement", StringComparison.OrdinalIgnoreCase);
        IsNew = item.IsNew;
    }

    public EventItem Item { get; }
    public string Title { get; }
    public string SeverityLabel { get; }
    public string RelativeTime { get; }
    public string SecondaryInfo { get; }
    public bool IsCritical { get; }
    public bool IsWarning { get; }
    public bool IsNew { get; }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..(max - 3)] + "...";
}

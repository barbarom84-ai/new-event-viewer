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
        IsCritical = EventSeverity.IsError(item.Level);
        IsWarning = EventSeverity.IsWarning(item.Level);
        IsNew = item.IsNew;
        NewBadge = Loc.T("Badge.New");
    }

    public EventItem Item { get; }
    public string Title { get; }
    public string SeverityLabel { get; }
    public string RelativeTime { get; }
    public string SecondaryInfo { get; }
    public bool IsCritical { get; }
    public bool IsWarning { get; }
    public bool IsNew { get; }
    public string NewBadge { get; }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..(max - 3)] + "...";
}

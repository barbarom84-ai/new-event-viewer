namespace EventViewer.Core;

public static class SeverityFilter
{
    public const string All = "all";
    public const string Critical = "critical";
    public const string Watch = "watch";

    public static bool Matches(string? filterId, string? eventLevel)
    {
        var id = string.IsNullOrWhiteSpace(filterId) ? All : filterId.Trim().ToLowerInvariant();
        return id switch
        {
            Critical => EventSeverity.IsError(eventLevel),
            Watch => EventSeverity.IsWarning(eventLevel),
            _ => true
        };
    }
}

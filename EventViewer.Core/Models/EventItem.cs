namespace EventViewer.Core;

/// <summary>
/// Neutral event model without UI framework dependencies.
/// </summary>
public sealed class EventItem
{
    public required string TimeCreated { get; init; }
    public DateTime? TimeCreatedAt { get; init; }
    public required string Level { get; init; }
    public required string Message { get; init; }
    public required string FullMessage { get; init; }
    public required string Source { get; init; }
    public int EventId { get; init; }
    public string? ExplanationTitle { get; init; }
    public string? ExplanationDescription { get; init; }
    public string? ExplanationSolution { get; init; }
    public string? ExplanationSeverity { get; init; }
    /// <summary>Readable COM/DCOM component name when resolved (e.g. Windows Update Medic).</summary>
    public string? RelatedComponentName { get; init; }
    /// <summary>CLSID/AppID GUID when present in the event.</summary>
    public string? RelatedComponentGuid { get; init; }
    /// <summary>Name + GUID on one line for UI.</summary>
    public string? RelatedComponentDisplay { get; init; }
    public bool HasRelatedComponent => !string.IsNullOrWhiteSpace(RelatedComponentDisplay);
    public TagInfo? Tag { get; init; }
    public bool HasTag => Tag != null;
    public bool IsNew { get; set; }
}

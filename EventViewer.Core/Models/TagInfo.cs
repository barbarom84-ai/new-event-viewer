namespace EventViewer.Core;

/// <summary>
/// Category tag for an event (UI-agnostic color as ARGB hex).
/// </summary>
public sealed class TagInfo
{
    public required string Name { get; init; }
    public required string[] Keywords { get; init; }
    /// <summary>ARGB hex, e.g. #FFF44336</summary>
    public required string ColorHex { get; init; }
    public required string Advice { get; init; }
}

namespace EventViewer.Core;

/// <summary>
/// Category tag for an event (UI-agnostic color as ARGB hex).
/// </summary>
public sealed class TagInfo
{
    /// <summary>Invariant key: Hardware, Network, Memory, Service, Security.</summary>
    public string Key { get; init; } = string.Empty;
    public required string Name { get; init; }
    public required string[] Keywords { get; init; }
    /// <summary>ARGB hex, e.g. #FFF44336</summary>
    public required string ColorHex { get; init; }
    public required string Advice { get; init; }
}

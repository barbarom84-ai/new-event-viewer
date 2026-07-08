using System.Diagnostics;
using System.Text.Json;

namespace EventViewer.Core;

/// <summary>
/// Local opt-in telemetry (JSONL file). Never crashes the app.
/// </summary>
public static class TelemetryService
{
    private static readonly object Sync = new();
    private static readonly JsonSerializerOptions JsonOptions = JsonDefaults.Create();

    private static bool _enabled;

    private static string TelemetryFilePath => Path.Combine(AppPaths.StateDirectory, "telemetry.log.jsonl");

    public static void Configure(bool enabled)
    {
        _enabled = enabled;
        if (_enabled)
        {
            AppPaths.EnsureDirectory(AppPaths.StateDirectory);
        }
    }

    public static bool IsEnabled() => _enabled;

    public static void Track(string eventName, object? payload = null)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        try
        {
            var evt = new TelemetryEvent
            {
                TimestampUtc = DateTime.UtcNow,
                EventName = eventName,
                SessionId = Process.GetCurrentProcess().Id.ToString(),
                PayloadJson = SerializePayload(payload)
            };

            var line = JsonSerializer.Serialize(evt, JsonOptions);
            lock (Sync)
            {
                File.AppendAllText(TelemetryFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Never break the app because of telemetry.
        }
    }

    public static void TrackException(string eventName, Exception exception)
    {
        Track(eventName, new Dictionary<string, string>
        {
            ["message"] = exception.Message,
            ["type"] = exception.GetType().Name
        });
    }

    private static string? SerializePayload(object? payload)
    {
        if (payload == null)
        {
            return null;
        }

        if (payload is string s)
        {
            return s;
        }

        if (payload is IReadOnlyDictionary<string, string> map)
        {
            return JsonSerializer.Serialize(map, JsonOptions);
        }

        if (payload is Dictionary<string, string> dict)
        {
            return JsonSerializer.Serialize(dict, JsonOptions);
        }

        if (payload is Dictionary<string, object?> loose)
        {
            var normalized = loose.ToDictionary(
                kv => kv.Key,
                kv => kv.Value?.ToString() ?? string.Empty);
            return JsonSerializer.Serialize(normalized, JsonOptions);
        }

        // Best-effort: ToString for unknown payloads (avoids anonymous-type reflection issues).
        return payload.ToString();
    }
}

public sealed class TelemetryEvent
{
    public DateTime TimestampUtc { get; init; }
    public required string EventName { get; init; }
    public required string SessionId { get; init; }
    public string? PayloadJson { get; init; }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace EventViewer
{
    /// <summary>
    /// Journalisation locale minimale (opt-in) pour suivre qualite/performance.
    /// </summary>
    public static class TelemetryService
    {
        private static readonly object Sync = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
                    Payload = payload
                };

                var line = JsonSerializer.Serialize(evt, JsonOptions);
                lock (Sync)
                {
                    File.AppendAllText(TelemetryFilePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Ne jamais casser l'app a cause de la telemetrie.
            }
        }

        public static void TrackException(string eventName, Exception exception)
        {
            Track(eventName, new
            {
                message = exception.Message,
                type = exception.GetType().Name
            });
        }
    }

    public sealed class TelemetryEvent
    {
        public DateTime TimestampUtc { get; init; }
        public required string EventName { get; init; }
        public required string SessionId { get; init; }
        public object? Payload { get; init; }
    }
}

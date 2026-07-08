using System.Text.Json;

namespace EventViewer.Core;

/// <summary>
/// Stores user feedback on recommendations (local JSONL).
/// </summary>
public static class RecommendationFeedbackStore
{
    private static readonly JsonSerializerOptions JsonOptions = JsonDefaults.Create();

    private static string FeedbackPath => Path.Combine(AppPaths.StateDirectory, "recommendation-feedback.jsonl");

    public static void Add(EventItem evt, bool useful)
    {
        if (evt == null)
        {
            return;
        }

        try
        {
            AppPaths.EnsureDirectory(AppPaths.StateDirectory);

            var record = new RecommendationFeedback
            {
                TimestampUtc = DateTime.UtcNow,
                EventId = evt.EventId,
                Source = evt.Source,
                Useful = useful
            };

            var line = JsonSerializer.Serialize(record, JsonOptions);
            File.AppendAllText(FeedbackPath, line + Environment.NewLine);
            TelemetryService.Track("recommendation_feedback", new Dictionary<string, string>
            {
                ["useful"] = useful ? "true" : "false",
                ["eventId"] = evt.EventId.ToString(),
                ["source"] = evt.Source
            });
        }
        catch
        {
            // Never crash the UI over feedback persistence.
        }
    }
}

public sealed class RecommendationFeedback
{
    public DateTime TimestampUtc { get; init; }
    public int EventId { get; init; }
    public required string Source { get; init; }
    public bool Useful { get; init; }
}

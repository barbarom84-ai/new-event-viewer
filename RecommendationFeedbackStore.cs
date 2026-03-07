using System;
using System.IO;
using System.Text.Json;

namespace EventViewer
{
    /// <summary>
    /// Stocke le feedback utilisateur sur les recommandations.
    /// </summary>
    public static class RecommendationFeedbackStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static string FeedbackPath => Path.Combine(AppPaths.StateDirectory, "recommendation-feedback.jsonl");

        public static void Add(EventLogItem evt, bool useful)
        {
            if (evt == null)
            {
                return;
            }

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
        }
    }

    public sealed class RecommendationFeedback
    {
        public DateTime TimestampUtc { get; init; }
        public int EventId { get; init; }
        public required string Source { get; init; }
        public bool Useful { get; init; }
    }
}

namespace EventViewer.Core
{
    /// <summary>
    /// Translates Windows event IDs into plain-language explanations (localized).
    /// </summary>
    public sealed class ErrorAnalyzer
    {
        private static readonly HashSet<int> KnownEventIds =
        [
            1, 7, 10, 11, 19, 20, 24, 41, 51, 153, 219, 372, 1000, 1001, 1002,
            1116, 1117, 2004, 2019, 4201, 4624, 4625, 4648, 6008, 7000, 7001,
            7009, 7011, 7031, 7034, 8003, 10016, 10110, 1074
        ];

        public EventExplanation GetExplanation(int eventId, string? eventSource = null)
        {
            if (eventId == 1001 && !IsBugcheckSource(eventSource))
            {
                return new EventExplanation
                {
                    Title = Loc.T("Event.Wer.Title"),
                    Description = Loc.T("Event.Wer.Description"),
                    Severity = Loc.T("Severity.InfoLabel"),
                    Solution = Loc.T("Event.Wer.Solution")
                };
            }

            if (Loc.Has($"Event.{eventId}.Title"))
            {
                var explanation = FromCatalog(eventId);

                // Richer shutdown guidance for Kernel-Power style events.
                if (eventId is 41 or 6008)
                {
                    explanation.Description = explanation.Description;
                    // Keep catalog solution for 1001-style richness when present; for 41/6008
                    // prefer multi-step guidance already in catalog for 41 if available.
                }

                return explanation;
            }

            return new EventExplanation
            {
                Title = Loc.T("Event.Unknown.Title", eventId),
                Description = Loc.T("Event.Unknown.Description", eventSource ?? "?"),
                Severity = Loc.T("Severity.UnknownLabel"),
                Solution = Loc.T("Event.Unknown.Solution")
            };
        }

        public Task<EventExplanation> EnrichWithAIAsync(int eventId, string eventMessage, string eventSource)
            => Task.FromResult(GetExplanation(eventId, eventSource));

        public string GetShortExplanation(int eventId)
        {
            if (Loc.Has($"Event.{eventId}.Title"))
            {
                return $"[ID {eventId}] {Loc.T($"Event.{eventId}.Title")}";
            }

            return $"[ID {eventId}] {Loc.T("Event.Unknown.Title", eventId)}";
        }

        public bool IsKnownEvent(int eventId) => KnownEventIds.Contains(eventId);

        public int GetKnownEventsCount() => KnownEventIds.Count;

        private static EventExplanation FromCatalog(int eventId)
            => new()
            {
                Title = Loc.T($"Event.{eventId}.Title"),
                Description = Loc.T($"Event.{eventId}.Description"),
                Severity = Loc.T($"Event.{eventId}.Severity"),
                Solution = Loc.T($"Event.{eventId}.Solution")
            };

        private static bool IsBugcheckSource(string? eventSource)
        {
            if (string.IsNullOrWhiteSpace(eventSource))
            {
                return true;
            }

            var s = eventSource.ToLowerInvariant();
            return s.Contains("bugcheck", StringComparison.Ordinal)
                   || s.Contains("systemerrorreporting", StringComparison.Ordinal)
                   || s.Contains("wer-system", StringComparison.Ordinal)
                   || s.Contains("kernel-power", StringComparison.Ordinal);
        }
    }

    public sealed class EventExplanation
    {
        public required string Title { get; init; }
        public required string Description { get; set; }
        public required string Severity { get; init; }
        public required string Solution { get; init; }
    }
}

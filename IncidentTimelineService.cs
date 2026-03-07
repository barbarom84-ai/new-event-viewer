using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EventViewer
{
    public sealed class IncidentTimelineService
    {
        public IReadOnlyList<IncidentBucket> BuildLast24Hours(IEnumerable<EventLogItem> events)
        {
            var source = events?.ToList() ?? new List<EventLogItem>();
            var now = DateTime.Now;
            var start = now.AddHours(-23);
            var buckets = new List<IncidentBucket>(24);

            for (var i = 0; i < 24; i++)
            {
                var hour = start.AddHours(i);
                buckets.Add(new IncidentBucket
                {
                    Hour = new DateTime(hour.Year, hour.Month, hour.Day, hour.Hour, 0, 0),
                    ErrorCount = 0,
                    WarningCount = 0
                });
            }

            foreach (var evt in source)
            {
                var parsed = TryResolveEventDate(evt);
                if (parsed == null)
                {
                    continue;
                }

                var rounded = new DateTime(parsed.Value.Year, parsed.Value.Month, parsed.Value.Day, parsed.Value.Hour, 0, 0);
                var match = buckets.FirstOrDefault(b => b.Hour == rounded);
                if (match == null)
                {
                    continue;
                }

                if (string.Equals(evt.Level, "Erreur", StringComparison.OrdinalIgnoreCase))
                {
                    match.ErrorCount++;
                }
                else
                {
                    match.WarningCount++;
                }
            }

            return buckets;
        }

        public static DateTime? TryResolveEventDate(EventLogItem evt)
        {
            if (evt == null)
            {
                return null;
            }

            if (evt.TimeCreatedAt.HasValue)
            {
                return evt.TimeCreatedAt.Value;
            }

            if (DateTime.TryParseExact(
                evt.TimeCreated,
                "dd/MM/yyyy HH:mm",
                CultureInfo.GetCultureInfo("fr-FR"),
                DateTimeStyles.AssumeLocal,
                out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }

    public sealed class IncidentBucket
    {
        public DateTime Hour { get; init; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int TotalCount => ErrorCount + WarningCount;
    }
}

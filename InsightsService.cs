using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventViewer
{
    /// <summary>
    /// Produit des insights actionnables pour prioriser les incidents.
    /// </summary>
    public sealed class InsightsService
    {
        public ProductInsights Build(IEnumerable<EventLogItem> events)
        {
            var source = events?.ToList() ?? new List<EventLogItem>();
            var critical = source.Count(e => string.Equals(e.Level, "Erreur", StringComparison.OrdinalIgnoreCase));
            var warnings = source.Count(e => string.Equals(e.Level, "Avertissement", StringComparison.OrdinalIgnoreCase));
            var tagged = source.Count(e => e.HasTag);

            var topSources = source
                .GroupBy(e => e.Source)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => new InsightMetric { Name = g.Key, Value = g.Count() })
                .ToList();

            var score = Math.Clamp((critical * 3) + (warnings * 1) + (tagged / 2), 0, 100);
            var severity = score switch
            {
                >= 70 => "Critique",
                >= 40 => "Elevée",
                >= 20 => "Modérée",
                _ => "Faible"
            };

            return new ProductInsights
            {
                RiskScore = score,
                SeverityLabel = severity,
                CriticalCount = critical,
                WarningCount = warnings,
                TaggedCount = tagged,
                TopSources = topSources
            };
        }

        public string BuildSummary(ProductInsights insights)
        {
            var sb = new StringBuilder();
            sb.AppendLine("INSIGHTS INCIDENTS");
            sb.AppendLine();
            sb.AppendLine($"Risque global: {insights.RiskScore}/100 ({insights.SeverityLabel})");
            sb.AppendLine($"Erreurs: {insights.CriticalCount}");
            sb.AppendLine($"Avertissements: {insights.WarningCount}");
            sb.AppendLine($"Evenements tags: {insights.TaggedCount}");
            sb.AppendLine();
            sb.AppendLine("Top sources:");
            foreach (var source in insights.TopSources)
            {
                sb.AppendLine($"- {source.Name}: {source.Value}");
            }

            return sb.ToString();
        }
    }

    public sealed class ProductInsights
    {
        public int RiskScore { get; init; }
        public required string SeverityLabel { get; init; }
        public int CriticalCount { get; init; }
        public int WarningCount { get; init; }
        public int TaggedCount { get; init; }
        public IReadOnlyList<InsightMetric> TopSources { get; init; } = Array.Empty<InsightMetric>();
    }

    public sealed class InsightMetric
    {
        public required string Name { get; init; }
        public int Value { get; init; }
    }
}

using System.Text;

namespace EventViewer.Core;

/// <summary>
/// Builds actionable insights to prioritize incidents.
/// </summary>
public sealed class InsightsService
{
    public ProductInsights Build(IEnumerable<EventItem> events)
    {
        var source = events?.ToList() ?? [];
        var critical = source.Count(e => EventSeverity.IsError(e.Level));
        var warnings = source.Count(e => EventSeverity.IsWarning(e.Level));
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
            >= 70 => Loc.T("Risk.Critical"),
            >= 40 => Loc.T("Risk.High"),
            >= 20 => Loc.T("Risk.Medium"),
            _ => Loc.T("Risk.Low")
        };

        return new ProductInsights
        {
            RiskScore = score,
            SeverityLabel = severity,
            HealthHeadline = HealthCopy.FromScore(score),
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

public static class HealthCopy
{
    public static string FromScore(int score) => score switch
    {
        >= 70 => Loc.T("Health.Action"),
        >= 40 => Loc.T("Health.Attention"),
        >= 20 => Loc.T("Health.Watch"),
        _ => Loc.T("Health.Ok")
    };

    public static string NoviceSeverity(string level)
    {
        if (EventSeverity.IsError(level))
        {
            return Loc.T("Severity.Critical");
        }

        if (EventSeverity.IsWarning(level))
        {
            return Loc.T("Severity.Watch");
        }

        return Loc.T("Severity.Info");
    }
}

public sealed class ProductInsights
{
    public int RiskScore { get; init; }
    public required string SeverityLabel { get; init; }
    public string HealthHeadline { get; init; } = Loc.T("Health.Ok");
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

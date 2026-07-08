using System.Globalization;
using System.Text;
using System.Text.Json;

namespace EventViewer.Core;

public sealed class ExportService
{
    private static readonly JsonSerializerOptions JsonOptions = JsonDefaults.Create(writeIndented: true);

    public async Task<string> ExportCsvAsync(IEnumerable<EventItem> events, string directory, CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureDirectory(directory);
        var path = Path.Combine(directory, $"incidents_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        var sb = new StringBuilder();
        sb.AppendLine("Date;Niveau;Id;Source;Titre;Message");

        foreach (var evt in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sb.AppendLine(string.Join(';',
                Escape(evt.TimeCreated),
                Escape(evt.Level),
                evt.EventId.ToString(CultureInfo.InvariantCulture),
                Escape(evt.Source),
                Escape(evt.ExplanationTitle ?? string.Empty),
                Escape(evt.Message)));
        }

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8, cancellationToken);
        return path;
    }

    public async Task<string> ExportJsonAsync(IEnumerable<EventItem> events, ProductInsights insights, string directory, CancellationToken cancellationToken = default)
    {
        AppPaths.EnsureDirectory(directory);
        var path = Path.Combine(directory, $"rapport_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        var payload = new ExportReport
        {
            GeneratedAt = DateTime.Now,
            Insights = insights,
            Events = events.Select(e => new ExportEventRow
            {
                TimeCreated = e.TimeCreated,
                Level = e.Level,
                EventId = e.EventId,
                Source = e.Source,
                Title = e.ExplanationTitle,
                Description = e.ExplanationDescription,
                Solution = e.ExplanationSolution,
                Message = e.Message
            }).ToList()
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await File.WriteAllTextAsync(path, json, Encoding.UTF8, cancellationToken);
        return path;
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(';') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

public sealed class ExportReport
{
    public DateTime GeneratedAt { get; init; }
    public required ProductInsights Insights { get; init; }
    public required List<ExportEventRow> Events { get; init; }
}

public sealed class ExportEventRow
{
    public required string TimeCreated { get; init; }
    public required string Level { get; init; }
    public int EventId { get; init; }
    public required string Source { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Solution { get; init; }
    public required string Message { get; init; }
}

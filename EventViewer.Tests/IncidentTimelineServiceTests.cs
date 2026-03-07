using EventViewer;
using System.Windows.Media;

namespace EventViewer.Tests;

public class IncidentTimelineServiceTests
{
    [Fact]
    public void BuildLast24Hours_AggregatesByHour()
    {
        var now = DateTime.Now;
        var items = new[]
        {
            CreateItem(now.AddMinutes(-5), "Erreur"),
            CreateItem(now.AddMinutes(-15), "Avertissement"),
            CreateItem(now.AddHours(-1), "Erreur")
        };

        var service = new IncidentTimelineService();
        var buckets = service.BuildLast24Hours(items);

        Assert.Equal(24, buckets.Count);
        Assert.True(buckets.Sum(b => b.TotalCount) >= 3);
    }

    private static EventLogItem CreateItem(DateTime date, string level)
    {
        return new EventLogItem
        {
            TimeCreated = date.ToString("dd/MM/yyyy HH:mm"),
            TimeCreatedAt = date,
            Level = level,
            LevelColor = Brushes.Red,
            LevelGlyph = "!",
            Message = "sample",
            Source = "test",
            EventId = 1
        };
    }
}

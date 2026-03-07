using EventViewer;
using System.Windows.Media;

namespace EventViewer.Tests;

public class InsightsServiceTests
{
    [Fact]
    public void Build_ComputesRiskScoreAndSeverity()
    {
        var events = new List<EventLogItem>
        {
            CreateItem("Erreur", "Disk"),
            CreateItem("Erreur", "Disk"),
            CreateItem("Avertissement", "Tcpip")
        };

        var service = new InsightsService();
        var insights = service.Build(events);

        Assert.True(insights.RiskScore > 0);
        Assert.False(string.IsNullOrWhiteSpace(insights.SeverityLabel));
        Assert.NotEmpty(insights.TopSources);
    }

    private static EventLogItem CreateItem(string level, string source)
    {
        return new EventLogItem
        {
            TimeCreated = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            TimeCreatedAt = DateTime.Now,
            Level = level,
            LevelColor = Brushes.Gold,
            LevelGlyph = "!",
            Message = "message",
            Source = source,
            EventId = 1000,
            Tag = new TagInfo
            {
                Name = "RÉSEAU",
                Keywords = new[] { "dns" },
                Color = Colors.Blue,
                Advice = "advice"
            }
        };
    }
}

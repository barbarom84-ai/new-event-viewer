using EventViewer.Core;

namespace EventViewer.Tests;

public class InsightsServiceTests
{
    [Fact]
    public void Build_ComputesRiskScoreAndSeverity()
    {
        var events = new List<EventItem>
        {
            CreateItem("Erreur", "Disk"),
            CreateItem("Erreur", "Disk"),
            CreateItem("Avertissement", "Tcpip")
        };

        var service = new InsightsService();
        var insights = service.Build(events);

        Assert.True(insights.RiskScore > 0);
        Assert.False(string.IsNullOrWhiteSpace(insights.SeverityLabel));
        Assert.False(string.IsNullOrWhiteSpace(insights.HealthHeadline));
        Assert.NotEmpty(insights.TopSources);
    }

    [Fact]
    public void HealthCopy_NoviceSeverity_MapsForNovices()
    {
        Assert.Equal("Critique", HealthCopy.NoviceSeverity("Erreur"));
        Assert.Equal("À surveiller", HealthCopy.NoviceSeverity("Avertissement"));
        Assert.Equal("Info", HealthCopy.NoviceSeverity("Information"));
    }

    private static EventItem CreateItem(string level, string source)
    {
        return new EventItem
        {
            TimeCreated = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            TimeCreatedAt = DateTime.Now,
            Level = level,
            Message = "message",
            FullMessage = "message",
            Source = source,
            EventId = 1000,
            Tag = new TagInfo
            {
                Name = "RÉSEAU",
                Keywords = ["dns"],
                ColorHex = "#FF2196F3",
                Advice = "advice"
            }
        };
    }
}

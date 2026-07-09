using EventViewer.Core;

namespace EventViewer.Tests;

public class InsightsServiceTests
{
    public InsightsServiceTests()
    {
        Loc.Initialize(AppLanguage.French);
    }

    [Fact]
    public void Build_ComputesRiskScoreAndSeverity()
    {
        Loc.Apply(AppLanguage.French);
        var events = new List<EventItem>
        {
            CreateItem(EventSeverity.Error, "Disk"),
            CreateItem(EventSeverity.Error, "Disk"),
            CreateItem(EventSeverity.Warning, "Tcpip")
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
        Loc.Apply(AppLanguage.French);
        Assert.Equal("Critique", HealthCopy.NoviceSeverity(EventSeverity.Error));
        Assert.Equal("À surveiller", HealthCopy.NoviceSeverity(EventSeverity.Warning));
        Assert.Equal("Info", HealthCopy.NoviceSeverity(EventSeverity.Information));
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
                Key = "Network",
                Name = Loc.T("Tag.Network"),
                Keywords = ["dns"],
                ColorHex = "#FF2196F3",
                Advice = "advice"
            }
        };
    }
}

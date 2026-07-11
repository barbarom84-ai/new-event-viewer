using EventViewer.Core;

namespace EventViewer.Tests;

public class SeverityFilterTests
{
    [Theory]
    [InlineData(SeverityFilter.All, EventSeverity.Error, true)]
    [InlineData(SeverityFilter.All, EventSeverity.Warning, true)]
    [InlineData(SeverityFilter.Critical, EventSeverity.Error, true)]
    [InlineData(SeverityFilter.Critical, EventSeverity.Warning, false)]
    [InlineData(SeverityFilter.Watch, EventSeverity.Warning, true)]
    [InlineData(SeverityFilter.Watch, EventSeverity.Error, false)]
    public void Matches_FiltersBySeverity(string filterId, string level, bool expected)
        => Assert.Equal(expected, SeverityFilter.Matches(filterId, level));
}

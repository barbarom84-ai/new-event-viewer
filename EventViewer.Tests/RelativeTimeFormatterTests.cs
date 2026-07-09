using EventViewer.Core;

namespace EventViewer.Tests;

public class RelativeTimeFormatterTests
{
    public RelativeTimeFormatterTests()
    {
        Loc.Initialize(AppLanguage.French);
    }

    [Fact]
    public void Format_RecentMinute_ReturnsFrenchRelative()
    {
        Loc.Apply(AppLanguage.French);
        var text = RelativeTimeFormatter.Format(DateTime.Now.AddMinutes(-3));
        Assert.Contains("il y a", text);
    }

    [Fact]
    public void Format_RecentMinute_ReturnsEnglishRelative()
    {
        Loc.Apply(AppLanguage.English);
        var text = RelativeTimeFormatter.Format(DateTime.Now.AddMinutes(-3));
        Assert.Contains("min ago", text);
    }
}

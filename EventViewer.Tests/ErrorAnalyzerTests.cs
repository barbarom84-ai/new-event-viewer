using EventViewer.Core;

namespace EventViewer.Tests;

public class ErrorAnalyzerTests
{
    public ErrorAnalyzerTests()
    {
        Loc.Initialize(AppLanguage.French);
    }

    [Fact]
    public void GetExplanation_KnownEvent_ReturnsCatalogEntry()
    {
        Loc.Apply(AppLanguage.French);
        var analyzer = new ErrorAnalyzer();
        var explanation = analyzer.GetExplanation(41, "Kernel-Power");

        Assert.False(string.IsNullOrWhiteSpace(explanation.Title));
        Assert.NotEqual(Loc.T("Severity.UnknownLabel"), explanation.Severity);
    }

    [Fact]
    public void GetExplanation_UnknownEvent_ReturnsFallback()
    {
        Loc.Apply(AppLanguage.French);
        var analyzer = new ErrorAnalyzer();
        var explanation = analyzer.GetExplanation(999999, "CustomSource");

        Assert.Contains("999999", explanation.Title);
        Assert.Contains("CustomSource", explanation.Description);
    }

    [Fact]
    public void GetExplanation_English_ReturnsTranslatedTitle()
    {
        Loc.Apply(AppLanguage.English);
        var explanation = new ErrorAnalyzer().GetExplanation(10016, "DCOM");
        Assert.Equal("Insufficient DCOM permissions", explanation.Title);
    }
}

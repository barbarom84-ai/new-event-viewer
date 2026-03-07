using EventViewer;

namespace EventViewer.Tests;

public class ErrorAnalyzerTests
{
    [Fact]
    public void GetExplanation_KnownEvent_ReturnsCatalogEntry()
    {
        var analyzer = new ErrorAnalyzer();
        var explanation = analyzer.GetExplanation(41, "Kernel-Power");

        Assert.False(string.IsNullOrWhiteSpace(explanation.Title));
        Assert.NotEqual("Inconnu", explanation.Severity);
    }

    [Fact]
    public void GetExplanation_UnknownEvent_ReturnsFallback()
    {
        var analyzer = new ErrorAnalyzer();
        var explanation = analyzer.GetExplanation(999999, "CustomSource");

        Assert.Contains("999999", explanation.Title);
        Assert.Contains("CustomSource", explanation.Description);
    }
}

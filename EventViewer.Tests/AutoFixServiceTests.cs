using EventViewer.Core;

namespace EventViewer.Tests;

public class AutoFixServiceTests
{
    private readonly AutoFixService _service = new();

    [Fact]
    public void Recommend_DnsIssue_SuggestsFlushDnsOnUsb()
    {
        var item = Create(8003, "DNS Client", "Erreur", "Problème de résolution DNS");
        var fix = _service.Recommend(item, isStoreBuild: false);
        Assert.Equal(AutoFixKind.FlushDns, fix.Kind);
        Assert.True(fix.CanExecuteAutomatically);
    }

    [Fact]
    public void Recommend_DnsIssue_UsesStoreSafeFallback()
    {
        var item = Create(8003, "DNS Client", "Erreur", "Problème de résolution DNS");
        var fix = _service.Recommend(item, isStoreBuild: true);
        Assert.Equal(AutoFixKind.OpenNetworkSettings, fix.Kind);
        Assert.True(fix.IsAvailableInStore);
    }

    [Fact]
    public void Recommend_LowDisk_SuggestsCleanup()
    {
        var item = Create(153, "Disk", "Avertissement", "Espace disque faible sur le volume");
        var fix = _service.Recommend(item, isStoreBuild: false);
        Assert.Equal(AutoFixKind.DiskCleanup, fix.Kind);
    }

    [Fact]
    public void Recommend_Dcom10016_DoesNotSuggestNetworkFix()
    {
        var item = Create(10016, "Microsoft-Windows-DistributedCOM", "Avertissement", "Permissions DCOM insuffisantes");
        var fix = _service.Recommend(item, isStoreBuild: false);
        Assert.Equal(AutoFixKind.None, fix.Kind);
        Assert.False(fix.CanExecuteAutomatically);
    }

    [Fact]
    public void Recommend_Bugcheck1001_SuggestsReliabilityHistory()
    {
        var item = Create(1001, "Microsoft-Windows-WER-SystemErrorReporting", "Critique", "bugcheck");
        var fix = _service.Recommend(item, isStoreBuild: false);
        Assert.Equal(AutoFixKind.OpenReliabilityHistory, fix.Kind);
        Assert.True(fix.CanExecuteAutomatically);
    }

    [Fact]
    public void GetExplanation_Bugcheck_ListsMultipleSteps()
    {
        var explanation = new ErrorAnalyzer().GetExplanation(1001, "Microsoft-Windows-WER-SystemErrorReporting");
        Assert.Contains("Historique de fiabilité", explanation.Solution);
        Assert.Contains("mises à jour Windows", explanation.Solution);
        Assert.Contains("SFC", explanation.Solution);
        Assert.Contains("mémoire", explanation.Solution, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BlueScreenView", explanation.Solution);
    }

    private static EventItem Create(int id, string source, string level, string message)
        => new()
        {
            TimeCreated = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            TimeCreatedAt = DateTime.Now,
            Level = level,
            Message = message,
            FullMessage = message,
            Source = source,
            EventId = id,
            ExplanationTitle = message
        };
}

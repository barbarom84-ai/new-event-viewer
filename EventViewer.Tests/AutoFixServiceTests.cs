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

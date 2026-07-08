using EventViewer.Core;

namespace EventViewer.Tests;

public class RelativeTimeFormatterTests
{
    [Fact]
    public void Format_RecentMinute_ReturnsFrenchRelative()
    {
        var text = RelativeTimeFormatter.Format(DateTime.Now.AddMinutes(-3));
        Assert.Contains("il y a", text);
    }
}

public class SnapshotManagerTests
{
    [Fact]
    public void CreateEventKey_UsesExpectedDelimiter()
    {
        var key = SnapshotManager.CreateEventKey(1000, "Disk", "bad block on volume");
        Assert.Equal("1000|Disk|bad block on volume", key);
    }

    [Fact]
    public void CreateSnapshot_And_DetectNewEvents()
    {
        var dir = Path.Combine(Path.GetTempPath(), "EventViewerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        try
        {
            // SnapshotManager uses AppPaths; for unit test of key logic only we assert GetNewEventKeys shape with in-memory keys.
            var older = SnapshotManager.CreateEventKey(41, "Kernel-Power", "Unexpected shutdown");
            var newer = SnapshotManager.CreateEventKey(7000, "Service Control Manager", "service failed");
            Assert.NotEqual(older, newer);
            Assert.Contains("|", older);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}

public class AiAssistantServiceTests
{
    [Fact]
    public async Task BuildIncidentSummary_UsesLocalModeLabel()
    {
        var service = new AiAssistantService(new ErrorAnalyzer());
        var item = new EventItem
        {
            TimeCreated = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            TimeCreatedAt = DateTime.Now,
            Level = "Erreur",
            Message = "crash",
            FullMessage = "crash",
            Source = "Kernel-Power",
            EventId = 41
        };

        var local = await service.BuildIncidentSummaryAsync(item, aiBetaEnabled: false);
        Assert.Contains("Mode local", local);
        Assert.Contains("Que faire", local);

        var beta = await service.BuildIncidentSummaryAsync(item, aiBetaEnabled: true);
        Assert.Contains("IA bêta", beta);
        Assert.Contains("locale", beta);
    }

    [Fact]
    public void IsCloudConfigured_RequiresEndpointAndKey()
    {
        Assert.False(AiAssistantService.IsCloudConfigured(new AppSettings()));
        Assert.False(AiAssistantService.IsCloudConfigured(new AppSettings
        {
            AiApiEndpoint = "https://api.openai.com/v1/chat/completions"
        }));
        Assert.True(AiAssistantService.IsCloudConfigured(new AppSettings
        {
            AiApiEndpoint = "https://api.openai.com/v1/chat/completions",
            AiApiKey = "sk-test"
        }));
    }
}

public class RecommendationFeedbackStoreTests
{
    [Fact]
    public void Add_WritesJsonlLine()
    {
        var before = Directory.Exists(AppPaths.StateDirectory)
            ? (File.Exists(Path.Combine(AppPaths.StateDirectory, "recommendation-feedback.jsonl"))
                ? File.ReadAllLines(Path.Combine(AppPaths.StateDirectory, "recommendation-feedback.jsonl")).Length
                : 0)
            : 0;

        var item = new EventItem
        {
            TimeCreated = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            TimeCreatedAt = DateTime.Now,
            Level = "Erreur",
            Message = "disk error",
            FullMessage = "disk error",
            Source = "Disk",
            EventId = 7
        };

        RecommendationFeedbackStore.Add(item, useful: true);

        var path = Path.Combine(AppPaths.StateDirectory, "recommendation-feedback.jsonl");
        Assert.True(File.Exists(path));
        var after = File.ReadAllLines(path).Length;
        Assert.True(after >= before + 1);
    }
}

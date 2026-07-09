using EventViewer.Core;

namespace EventViewer.Tests;

public class AiAssistantServiceTests
{
    public AiAssistantServiceTests()
    {
        Loc.Initialize(AppLanguage.French);
    }

    [Fact]
    public async Task BuildIncidentSummary_UsesLocalModeLabel()
    {
        Loc.Apply(AppLanguage.French);
        var service = new AiAssistantService(new ErrorAnalyzer());
        var item = new EventItem
        {
            TimeCreated = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            TimeCreatedAt = DateTime.Now,
            Level = EventSeverity.Error,
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
        Assert.False(AiAssistantService.IsCloudConfigured(new AppSettings
        {
            AiApiEndpoint = "http://api.openai.com/v1/chat/completions",
            AiApiKey = "sk-test"
        }));
    }
}

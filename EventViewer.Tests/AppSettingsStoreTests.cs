using EventViewer.Core;

namespace EventViewer.Tests;

public class AppSettingsStoreTests
{
    [Fact]
    public void TrySave_And_Load_RoundTripsTelemetryFlag()
    {
        var settings = new AppSettings
        {
            TelemetryOptIn = true,
            AiBetaEnabled = false,
            AiApiEndpoint = "https://api.openai.com/v1/chat/completions",
            AiModel = "gpt-4o-mini"
        };

        var saved = AppSettingsStore.TrySave(settings, out var error);
        Assert.True(saved, error);
        Assert.Null(error);

        var loaded = AppSettingsStore.Load();
        Assert.True(loaded.TelemetryOptIn);
        Assert.Equal("gpt-4o-mini", loaded.AiModel);
    }
}

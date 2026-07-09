using EventViewer.Core;

namespace EventViewer.Tests;

public class SecurityHelpersTests
{
    [Theory]
    [InlineData("https://api.openai.com/v1/chat/completions", true)]
    [InlineData("http://api.openai.com/v1/chat/completions", false)]
    [InlineData("https://localhost/v1", false)]
    [InlineData("https://127.0.0.1/v1", false)]
    [InlineData("https://192.168.1.10/v1", false)]
    [InlineData("https://10.0.0.5/chat", false)]
    [InlineData("https://user:pass@evil.com/v1", false)]
    [InlineData("ftp://example.com/x", false)]
    public void AiEndpointPolicy_ValidatesExpectedHosts(string endpoint, bool expected)
    {
        Assert.Equal(expected, AiEndpointPolicy.IsAllowed(endpoint));
    }

    [Fact]
    public void SecretProtector_RoundTrips()
    {
        var protectedValue = SecretProtector.Protect("sk-test-secret");
        Assert.False(string.IsNullOrWhiteSpace(protectedValue));
        Assert.DoesNotContain("sk-test-secret", protectedValue);
        Assert.Equal("sk-test-secret", SecretProtector.Unprotect(protectedValue));
    }

    [Fact]
    public void AppSettings_DoesNotSerializePlaintextApiKey()
    {
        var settings = new AppSettings();
        settings.SetApiKey("sk-should-not-appear");
        settings.PrepareForPersist();

        var json = System.Text.Json.JsonSerializer.Serialize(settings, JsonDefaults.Create());
        Assert.DoesNotContain("sk-should-not-appear", json);
        Assert.Contains("aiApiKeyProtected", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"aiApiKey\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"AiApiKey\":", json);
    }

    [Fact]
    public void IsCloudConfigured_RejectsHttpEndpoint()
    {
        Assert.False(AiAssistantService.IsCloudConfigured(new AppSettings
        {
            AiApiEndpoint = "http://api.openai.com/v1/chat/completions",
            AiApiKey = "sk-test"
        }));
    }

    [Fact]
    public async Task OpenUriAsync_RejectsUnknownUri()
    {
        var result = await SystemMaintenanceHelper.OpenUriAsync("https://evil.example/payload");
        Assert.False(result.Success);
        Assert.Contains("non autorisée", result.Error);
    }
}

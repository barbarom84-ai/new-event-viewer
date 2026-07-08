using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EventViewer.Core;

/// <summary>
/// Local explanation assistant with optional OpenAI-compatible cloud endpoint.
/// Always falls back to the offline catalog when cloud is unavailable.
/// </summary>
public sealed class AiAssistantService
{
    private readonly ErrorAnalyzer _errorAnalyzer;
    private readonly HttpClient _httpClient;

    public AiAssistantService(ErrorAnalyzer errorAnalyzer, HttpClient? httpClient = null)
    {
        _errorAnalyzer = errorAnalyzer;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
    }

    public async Task<string> BuildIncidentSummaryAsync(EventItem? evt, AppSettings settings, CancellationToken cancellationToken = default)
    {
        if (evt == null)
        {
            return "Aucun incident sélectionné.";
        }

        var local = BuildLocalSummary(evt, settings.AiBetaEnabled);

        if (!settings.AiBetaEnabled || !IsCloudConfigured(settings))
        {
            return local;
        }

        try
        {
            var cloud = await BuildCloudSummaryAsync(evt, settings, cancellationToken);
            if (!string.IsNullOrWhiteSpace(cloud))
            {
                TelemetryService.Track("assistant_cloud_success", new Dictionary<string, string>
                {
                    ["eventId"] = evt.EventId.ToString()
                });
                return cloud;
            }
        }
        catch (Exception ex)
        {
            TelemetryService.TrackException("assistant_cloud_failed", ex);
        }

        return local + "\n\n(Aucune réponse cloud : aperçu local ci-dessus.)";
    }

    /// <summary>Backward-compatible overload used by older callers/tests. </summary>
    public Task<string> BuildIncidentSummaryAsync(EventItem? evt, bool aiBetaEnabled)
        => BuildIncidentSummaryAsync(evt, new AppSettings { AiBetaEnabled = aiBetaEnabled });

    public static bool IsCloudConfigured(AppSettings settings)
    {
        var endpoint = settings.AiApiEndpoint?.Trim() ?? string.Empty;
        var key = ResolveApiKey(settings);
        return endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(key);
    }

    private static string ResolveApiKey(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.AiApiKey))
        {
            return settings.AiApiKey.Trim();
        }

        return Environment.GetEnvironmentVariable("EVENTVIEWER_AI_API_KEY")?.Trim() ?? string.Empty;
    }

    private string BuildLocalSummary(EventItem evt, bool aiBetaEnabled)
    {
        var explanation = _errorAnalyzer.GetExplanation(evt.EventId, evt.Source);
        var mode = aiBetaEnabled
            ? "Mode IA bêta (explication locale — cloud non utilisé pour cette réponse)"
            : "Mode local";

        return
            $"{mode}\n\n" +
            $"En bref : {explanation.Title}\n\n" +
            $"Ce que ça signifie :\n{explanation.Description}\n\n" +
            $"Que faire :\n{explanation.Solution}\n\n" +
            $"Détail : {evt.Level} · {evt.Source} · code {evt.EventId}";
    }

    private async Task<string?> BuildCloudSummaryAsync(EventItem evt, AppSettings settings, CancellationToken cancellationToken)
    {
        var explanation = _errorAnalyzer.GetExplanation(evt.EventId, evt.Source);
        var endpoint = settings.AiApiEndpoint.Trim();
        var model = string.IsNullOrWhiteSpace(settings.AiModel) ? "gpt-4o-mini" : settings.AiModel.Trim();
        var apiKey = ResolveApiKey(settings);

        var userPrompt =
            "Explique cet événement Windows en français simple pour un utilisateur non technique. " +
            "Réponds avec exactement 3 sections : En bref / Ce que ça signifie / Que faire. " +
            "Sois concret et rassurant sans inventer de faits.\n\n" +
            $"ID: {evt.EventId}\n" +
            $"Niveau: {evt.Level}\n" +
            $"Source: {evt.Source}\n" +
            $"Catalogue: {explanation.Title}\n" +
            $"Description catalogue: {explanation.Description}\n" +
            $"Solution catalogue: {explanation.Solution}\n" +
            $"Message: {Truncate(evt.FullMessage, 1200)}";

        var requestBody = new AiChatRequest
        {
            Model = model,
            Temperature = 0.2,
            Messages =
            [
                new AiChatMessage { Role = "system", Content = "Tu es un assistant de diagnostic Windows pour débutants." },
                new AiChatMessage { Role = "user", Content = userPrompt }
            ]
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonDefaults.Create()),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"API IA HTTP {(int)response.StatusCode}: {Truncate(body, 200)}");
        }

        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            return null;
        }

        var content = choices[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        return
            "Mode IA cloud\n\n" +
            content.Trim() +
            $"\n\nDétail : {evt.Level} · {evt.Source} · code {evt.EventId}";
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
        {
            return value ?? string.Empty;
        }

        return value[..(max - 1)] + "…";
    }
}

internal sealed class AiChatRequest
{
    public required string Model { get; init; }
    public double Temperature { get; init; }
    public required List<AiChatMessage> Messages { get; init; }
}

internal sealed class AiChatMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}

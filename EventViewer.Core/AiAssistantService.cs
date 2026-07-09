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
        _httpClient = httpClient ?? CreateDefaultHttpClient();
    }

    public async Task<string> BuildIncidentSummaryAsync(EventItem? evt, AppSettings settings, CancellationToken cancellationToken = default)
    {
        if (evt == null)
        {
            return Loc.T("Ai.NoSelection");
        }

        var local = BuildLocalSummary(evt, settings.AiBetaEnabled);

        if (!settings.AiBetaEnabled || !IsCloudConfigured(settings))
        {
            return local;
        }

        if (!settings.AiCloudDataConsent)
        {
            return local + "\n\n(Cloud désactivé : acceptez l'envoi des détails d'incident vers l'endpoint configuré.)";
        }

        if (!AiEndpointPolicy.TryValidate(settings.AiApiEndpoint, out var endpointUri, out var endpointError))
        {
            return local + $"\n\n(Endpoint IA refusé : {endpointError})";
        }

        try
        {
            var cloud = await BuildCloudSummaryAsync(evt, settings, endpointUri!, cancellationToken);
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

    /// <summary> Backward-compatible overload used by older callers/tests. </summary>
    public Task<string> BuildIncidentSummaryAsync(EventItem? evt, bool aiBetaEnabled)
        => BuildIncidentSummaryAsync(evt, new AppSettings { AiBetaEnabled = aiBetaEnabled });

    public static bool IsCloudConfigured(AppSettings settings)
    {
        var key = ResolveApiKey(settings);
        return AiEndpointPolicy.IsAllowed(settings.AiApiEndpoint) && !string.IsNullOrWhiteSpace(key);
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };

        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(25) };
    }

    private static string ResolveApiKey(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.AiApiKey))
        {
            return settings.AiApiKey.Trim();
        }

        if (!string.IsNullOrWhiteSpace(settings.AiApiKeyProtected))
        {
            var fromStore = SecretProtector.Unprotect(settings.AiApiKeyProtected);
            if (!string.IsNullOrWhiteSpace(fromStore))
            {
                return fromStore;
            }
        }

        return Environment.GetEnvironmentVariable("EVENTVIEWER_AI_API_KEY")?.Trim() ?? string.Empty;
    }

    private string BuildLocalSummary(EventItem evt, bool aiBetaEnabled)
    {
        var explanation = _errorAnalyzer.GetExplanation(evt.EventId, evt.Source);
        var mode = aiBetaEnabled
            ? Loc.T("Ai.BetaLocal")
            : Loc.T("Ai.LocalMode");

        var componentLine = string.IsNullOrWhiteSpace(evt.RelatedComponentDisplay)
            ? string.Empty
            : $"\n\n{Loc.T("Detail.Component")} : {evt.RelatedComponentDisplay}";

        return
            $"{mode}\n\n" +
            $"{Loc.T("Ai.InBrief")} : {explanation.Title}\n\n" +
            $"{Loc.T("Ai.Meaning")} :\n{explanation.Description}{componentLine}\n\n" +
            $"{Loc.T("Ai.WhatToDo")} :\n{explanation.Solution}\n\n" +
            $"{Loc.T("Ai.Detail")} : {EventSeverity.Display(evt.Level)} · {evt.Source} · {evt.EventId}";
    }

    private async Task<string?> BuildCloudSummaryAsync(
        EventItem evt,
        AppSettings settings,
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        var explanation = _errorAnalyzer.GetExplanation(evt.EventId, evt.Source);
        var model = string.IsNullOrWhiteSpace(settings.AiModel) ? "gpt-4o-mini" : settings.AiModel.Trim();
        if (model.Length > 64 || model.Any(c => !(char.IsLetterOrDigit(c) || c is '-' or '_' or '.')))
        {
            throw new InvalidOperationException("Nom de modèle IA invalide.");
        }

        var apiKey = ResolveApiKey(settings);

        var userPrompt =
            $"Explain this Windows event in {Loc.T("Ai.PromptLang")} for a non-technical user. " +
            $"Reply with exactly 3 sections: {Loc.T("Ai.InBrief")} / {Loc.T("Ai.Meaning")} / {Loc.T("Ai.WhatToDo")}. " +
            "Be concrete and reassuring without inventing facts.\n\n" +
            $"ID: {evt.EventId}\n" +
            $"Level: {EventSeverity.Display(evt.Level)}\n" +
            $"Source: {Truncate(evt.Source, 120)}\n" +
            $"Component: {Truncate(evt.RelatedComponentDisplay ?? "-", 200)}\n" +
            $"Catalog: {Truncate(explanation.Title, 200)}\n" +
            $"Catalog description: {Truncate(explanation.Description, 400)}\n" +
            $"Catalog solution: {Truncate(explanation.Solution, 400)}\n" +
            $"Message: {Truncate(evt.FullMessage, 800)}";

        var requestBody = new AiChatRequest
        {
            Model = model,
            Temperature = 0.2,
            Messages =
            [
                new AiChatMessage { Role = "system", Content = "You are a Windows diagnostic assistant for beginners." },
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
            Loc.T("Ai.CloudMode") + "\n\n" +
            Truncate(content.Trim(), 4000) +
            $"\n\n{Loc.T("Ai.Detail")} : {EventSeverity.Display(evt.Level)} · {evt.Source} · {evt.EventId}";
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

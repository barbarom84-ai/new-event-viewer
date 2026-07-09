using System;

namespace EventViewer.Core
{
    /// <summary>
    /// Parametres applicatifs persistants.
    /// </summary>
    public sealed class AppSettings
    {
        public bool TelemetryOptIn { get; set; } = false;
        public bool AiBetaEnabled { get; set; } = false;

        /// <summary>
        /// User acknowledged that event text may be sent to the configured cloud endpoint.
        /// </summary>
        public bool AiCloudDataConsent { get; set; } = false;

        /// <summary>
        /// Optional OpenAI-compatible chat completions endpoint (HTTPS only).
        /// Example: https://api.openai.com/v1/chat/completions
        /// </summary>
        public string AiApiEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Transient plaintext API key (never written to disk). Prefer EVENTVIEWER_AI_API_KEY.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string AiApiKey { get; set; } = string.Empty;

        /// <summary>
        /// DPAPI-protected API key blob persisted in appsettings.json.
        /// </summary>
        public string AiApiKeyProtected { get; set; } = string.Empty;

        public string AiModel { get; set; } = "gpt-4o-mini";

        /// <summary>
        /// UI language preference: "system", "fr", "en", or "it".
        /// </summary>
        public string UiLanguage { get; set; } = AppLanguage.System;

        /// <summary>
        /// UI theme: "dark", "light", "midnight", or "forest".
        /// </summary>
        public string UiTheme { get; set; } = "dark";

        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        public void SetApiKey(string? plaintext)
        {
            AiApiKey = plaintext?.Trim() ?? string.Empty;
            AiApiKeyProtected = string.IsNullOrEmpty(AiApiKey)
                ? string.Empty
                : SecretProtector.Protect(AiApiKey);
        }

        public void HydrateSecretsFromStore()
        {
            if (!string.IsNullOrEmpty(AiApiKeyProtected))
            {
                AiApiKey = SecretProtector.Unprotect(AiApiKeyProtected);
            }
        }

        public void PrepareForPersist()
        {
            // Prefer re-protecting any in-memory key change; never keep plaintext in JSON.
            if (!string.IsNullOrEmpty(AiApiKey))
            {
                AiApiKeyProtected = SecretProtector.Protect(AiApiKey);
            }
        }
    }
}

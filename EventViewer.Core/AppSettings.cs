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
        /// Optional OpenAI-compatible chat completions endpoint.
        /// Example: https://api.openai.com/v1/chat/completions
        /// </summary>
        public string AiApiEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Optional API key. Prefer environment variable EVENTVIEWER_AI_API_KEY when empty.
        /// </summary>
        public string AiApiKey { get; set; } = string.Empty;

        public string AiModel { get; set; } = "gpt-4o-mini";

        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}

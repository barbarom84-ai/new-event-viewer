using System;

namespace EventViewer
{
    /// <summary>
    /// Parametres applicatifs persistants.
    /// </summary>
    public sealed class AppSettings
    {
        public bool TelemetryOptIn { get; set; } = false;
        public bool AiBetaEnabled { get; set; } = false;
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    }
}

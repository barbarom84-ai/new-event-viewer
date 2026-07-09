using System;
using System.IO;
using System.Text.Json;

namespace EventViewer.Core
{
    /// <summary>
    /// Charge/enregistre les parametres dans le dossier d'etat de l'application.
    /// </summary>
    public static class AppSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = JsonDefaults.Create(writeIndented: true);
        private static readonly object Sync = new();

        private static string SettingsFilePath => Path.Combine(AppPaths.StateDirectory, "appsettings.json");

        public static AppSettings Load()
        {
            try
            {
                var filePath = SettingsFilePath;
                if (!File.Exists(filePath))
                {
                    return new AppSettings();
                }

                var json = SafeFileIO.ReadAllTextLimited(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new AppSettings();
                }

                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                settings.HydrateSecretsFromStore();
                MigrateLegacyPlaintextKey(settings, json);
                return settings;
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static bool TrySave(AppSettings settings, out string? error)
        {
            error = null;
            if (settings == null)
            {
                return true;
            }

            try
            {
                settings.PrepareForPersist();
                settings.LastUpdatedUtc = DateTime.UtcNow;
                AppPaths.EnsureDirectory(AppPaths.StateDirectory);
                var json = JsonSerializer.Serialize(settings, JsonOptions);
                lock (Sync)
                {
                    File.WriteAllText(SettingsFilePath, json);
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static void Save(AppSettings settings)
            => TrySave(settings, out _);

        /// <summary>
        /// One-shot migration: if an old plaintext AiApiKey field exists in JSON, encrypt it and rewrite.
        /// </summary>
        private static void MigrateLegacyPlaintextKey(AppSettings settings, string rawJson)
        {
            if (!string.IsNullOrEmpty(settings.AiApiKeyProtected) || string.IsNullOrEmpty(rawJson))
            {
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                if (!doc.RootElement.TryGetProperty("AiApiKey", out var keyProp) &&
                    !doc.RootElement.TryGetProperty("aiApiKey", out keyProp))
                {
                    return;
                }

                var legacy = keyProp.GetString()?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(legacy))
                {
                    return;
                }

                settings.SetApiKey(legacy);
                TrySave(settings, out _);
            }
            catch
            {
                // Ignore migration failures; user can re-enter the key.
            }
        }
    }
}

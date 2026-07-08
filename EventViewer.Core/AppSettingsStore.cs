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

                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
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
    }
}

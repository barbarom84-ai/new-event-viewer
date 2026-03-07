using System;
using System.IO;
using System.Text.Json;

namespace EventViewer
{
    /// <summary>
    /// Charge/enregistre les parametres dans le dossier d'etat de l'application.
    /// </summary>
    public static class AppSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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

        public static void Save(AppSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.LastUpdatedUtc = DateTime.UtcNow;
            AppPaths.EnsureDirectory(AppPaths.StateDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
    }
}

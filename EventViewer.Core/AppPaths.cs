using System;
using System.IO;

namespace EventViewer.Core
{
    /// <summary>
    /// Chemins d'écriture : toujours vers des emplacements utilisateur accessibles
    /// (évite les crashs quand le dossier de l'exe est en lecture seule).
    /// </summary>
    public static class AppPaths
    {
        private const string AppFolderName = "EventBeaconTool";

        public static string StateDirectory
        {
            get
            {
                var preferred = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    AppFolderName,
                    "State");

                // USB legacy: if next-to-exe is writable and already has state, keep using it.
                try
                {
                    var portable = AppDomain.CurrentDomain.BaseDirectory;
                    if (IsWritableDirectory(portable) &&
                        (File.Exists(Path.Combine(portable, "appsettings.json")) ||
                         File.Exists(Path.Combine(portable, ".event_snapshot.json"))))
                    {
                        return portable;
                    }
                }
                catch
                {
                    // Fall through to AppData.
                }

                return preferred;
            }
        }

        public static string SnapshotFilePath => Path.Combine(StateDirectory, ".event_snapshot.json");

        public static string ExportDirectory
        {
            get
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (!string.IsNullOrWhiteSpace(docs))
                {
                    return Path.Combine(docs, AppFolderName, "Exports");
                }

                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    AppFolderName,
                    "Exports");
            }
        }

        public static void EnsureDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return;
            }

            Directory.CreateDirectory(directoryPath);
        }

        public static bool TryEnsureWritableDirectory(string directoryPath, out string? error)
        {
            error = null;
            try
            {
                EnsureDirectory(directoryPath);
                var probe = Path.Combine(directoryPath, $".write_probe_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool IsWritableDirectory(string directoryPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                {
                    return false;
                }

                var probe = Path.Combine(directoryPath, $".write_probe_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

using System;
using System.IO;

namespace EventViewer.Core
{
    /// <summary>
    /// Chemins d'écriture utilisateur (toujours accessibles, USB et Store).
    /// </summary>
    public static class AppPaths
    {
        private const string AppFolderName = "WinBeacon";

        public static string StateDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppFolderName,
                "State");

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
    }
}

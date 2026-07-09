using System;
using System.IO;

namespace EventViewer
{
    /// <summary>
    /// Centralise les chemins d'écriture (Store-friendly vs version portable USB).
    /// </summary>
    public static class AppPaths
    {
        private const string AppFolderName = "WinBeacon";

        public static string StateDirectory
        {
            get
            {
#if STORE_BUILD
                var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(baseDir, AppFolderName, "State");
#else
                // Version USB : portable, écrit dans le dossier de l'exe
                return AppDomain.CurrentDomain.BaseDirectory;
#endif
            }
        }

        public static string SnapshotFilePath => Path.Combine(StateDirectory, ".event_snapshot.json");

        public static string ExportDirectory
        {
            get
            {
#if STORE_BUILD
                // Plus accessible pour l'utilisateur qu'un dossier AppData
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.Combine(docs, AppFolderName, "Exports");
#else
                return AppDomain.CurrentDomain.BaseDirectory;
#endif
            }
        }

        public static void EnsureDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                return;

            Directory.CreateDirectory(directoryPath);
        }
    }
}



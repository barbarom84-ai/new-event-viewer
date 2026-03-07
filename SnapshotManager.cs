using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EventViewer
{
    /// <summary>
    /// Gère les snapshots des événements système pour comparer l'évolution des erreurs.
    /// </summary>
    public sealed class SnapshotManager
    {
        private const string SnapshotFileName = ".event_snapshot.json";
        private readonly string _snapshotPath;

        public SnapshotManager()
        {
            _snapshotPath = AppPaths.SnapshotFilePath;
        }

        /// <summary>
        /// Crée un snapshot de l'état actuel des événements.
        /// </summary>
        public void CreateSnapshot(IEnumerable<EventLogItem> events)
        {
            // S'assure que le dossier cible existe (Store-friendly / USB)
            AppPaths.EnsureDirectory(Path.GetDirectoryName(_snapshotPath) ?? AppPaths.StateDirectory);

            var snapshot = new EventSnapshot
            {
                CreatedAt = DateTime.Now,
                Events = events.Select(e => new SnapshotEvent
                {
                    EventId = e.EventId,
                    Source = e.Source,
                    TimeCreated = e.TimeCreated,
                    Message = e.Message,
                    Level = e.Level
                }).ToList()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(snapshot, options);
            
            try
            {
                // Si le fichier existe et est caché, on retire l'attribut pour pouvoir écrire
                if (File.Exists(_snapshotPath))
                {
                    File.SetAttributes(_snapshotPath, FileAttributes.Normal);
                }

                // Créer ou écraser le fichier
                File.WriteAllText(_snapshotPath, json);
                
                // Marquer à nouveau le fichier comme caché sur Windows
                var fileInfo = new FileInfo(_snapshotPath);
                fileInfo.Attributes |= FileAttributes.Hidden;
            }
            catch (Exception ex)
            {
                // Si on ne peut pas gérer les attributs, on essaie au moins d'écrire normalement
                try
                {
                    File.WriteAllText(_snapshotPath, json);
                }
                catch
                {
                    throw new Exception($"Impossible d'accéder au fichier snapshot : {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Charge le dernier snapshot s'il existe.
        /// </summary>
        public EventSnapshot? LoadSnapshot()
        {
            if (!File.Exists(_snapshotPath))
                return null;

            try
            {
                var json = File.ReadAllText(_snapshotPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                return JsonSerializer.Deserialize<EventSnapshot>(json, options);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Compare les événements actuels avec le snapshot pour identifier les nouvelles erreurs.
        /// </summary>
        public HashSet<string> GetNewEventKeys(IEnumerable<EventLogItem> currentEvents)
        {
            var snapshot = LoadSnapshot();
            if (snapshot == null)
                return new HashSet<string>();

            // Créer une clé unique pour chaque événement (EventId + Source + Message tronqué)
            var snapshotKeys = new HashSet<string>(
                snapshot.Events.Select(e => CreateEventKey(e.EventId, e.Source, e.Message))
            );

            var newEvents = new HashSet<string>();
            foreach (var evt in currentEvents)
            {
                var key = CreateEventKey(evt.EventId, evt.Source, evt.Message);
                if (!snapshotKeys.Contains(key))
                {
                    newEvents.Add(key);
                }
            }

            return newEvents;
        }

        /// <summary>
        /// Crée une clé unique pour identifier un événement.
        /// </summary>
        public static string CreateEventKey(int eventId, string source, string message)
        {
            // Utiliser les 50 premiers caractères du message pour éviter les duplications
            var shortMessage = message.Length > 50 ? message.Substring(0, 50) : message;
            return $"{eventId}|{source}|{shortMessage}";
        }

        /// <summary>
        /// Vérifie si un snapshot existe.
        /// </summary>
        public bool SnapshotExists() => File.Exists(_snapshotPath);

        /// <summary>
        /// Obtient la date de création du dernier snapshot.
        /// </summary>
        public DateTime? GetSnapshotDate()
        {
            var snapshot = LoadSnapshot();
            return snapshot?.CreatedAt;
        }

        /// <summary>
        /// Supprime le snapshot actuel.
        /// </summary>
        public void DeleteSnapshot()
        {
            if (File.Exists(_snapshotPath))
            {
                File.Delete(_snapshotPath);
            }
        }
    }

    /// <summary>
    /// Représente un snapshot d'événements à un instant T.
    /// </summary>
    public sealed class EventSnapshot
    {
        public DateTime CreatedAt { get; set; }
        public List<SnapshotEvent> Events { get; set; } = new();
    }

    /// <summary>
    /// Représente un événement dans un snapshot.
    /// </summary>
    public sealed class SnapshotEvent
    {
        public int EventId { get; set; }
        public string Source { get; set; } = string.Empty;
        public string TimeCreated { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
    }
}


using System.Text.Json;

namespace EventViewer.Core;

/// <summary>
/// Snapshots for comparing incident evolution over time.
/// </summary>
public sealed class SnapshotManager
{
    private readonly string _snapshotPath;

    public SnapshotManager()
    {
        _snapshotPath = AppPaths.SnapshotFilePath;
    }

    public void CreateSnapshot(IEnumerable<EventItem> events)
    {
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

        var options = JsonDefaults.Create(writeIndented: true);
        var json = JsonSerializer.Serialize(snapshot, options);

        try
        {
            if (File.Exists(_snapshotPath))
            {
                File.SetAttributes(_snapshotPath, FileAttributes.Normal);
            }

            File.WriteAllText(_snapshotPath, json);
            var fileInfo = new FileInfo(_snapshotPath);
            fileInfo.Attributes |= FileAttributes.Hidden;
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(_snapshotPath, json);
            }
            catch
            {
                throw new InvalidOperationException($"Impossible d'accéder au fichier snapshot : {ex.Message}", ex);
            }
        }
    }

    public EventSnapshot? LoadSnapshot()
    {
        if (!File.Exists(_snapshotPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_snapshotPath);
                return JsonSerializer.Deserialize<EventSnapshot>(json, JsonDefaults.Create());
        }
        catch
        {
            return null;
        }
    }

    public HashSet<string> GetNewEventKeys(IEnumerable<EventItem> currentEvents)
    {
        var snapshot = LoadSnapshot();
        if (snapshot == null)
        {
            return [];
        }

        var snapshotKeys = new HashSet<string>(
            snapshot.Events.Select(e => CreateEventKey(e.EventId, e.Source, e.Message)),
            StringComparer.Ordinal);

        var newEvents = new HashSet<string>(StringComparer.Ordinal);
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

    public static string CreateEventKey(int eventId, string source, string message)
    {
        var shortMessage = message.Length > 50 ? message[..50] : message;
        return $"{eventId}|{source}|{shortMessage}";
    }

    public bool SnapshotExists() => File.Exists(_snapshotPath);

    public DateTime? GetSnapshotDate() => LoadSnapshot()?.CreatedAt;

    public void DeleteSnapshot()
    {
        if (File.Exists(_snapshotPath))
        {
            File.SetAttributes(_snapshotPath, FileAttributes.Normal);
            File.Delete(_snapshotPath);
        }
    }
}

public sealed class EventSnapshot
{
    public DateTime CreatedAt { get; set; }
    public List<SnapshotEvent> Events { get; set; } = [];
}

public sealed class SnapshotEvent
{
    public int EventId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string TimeCreated { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
}

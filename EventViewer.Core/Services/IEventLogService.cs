namespace EventViewer.Core;

public interface IEventLogService
{
    Task<IReadOnlyList<EventItem>> LoadRecentAsync(string logName, int maxEvents = 200, CancellationToken cancellationToken = default);
}

public sealed class WindowsEventLogService : IEventLogService
{
    private readonly ErrorAnalyzer _errorAnalyzer;
    private readonly TagDetector _tagDetector;

    public WindowsEventLogService(ErrorAnalyzer errorAnalyzer, TagDetector tagDetector)
    {
        _errorAnalyzer = errorAnalyzer;
        _tagDetector = tagDetector;
    }

    public Task<IReadOnlyList<EventItem>> LoadRecentAsync(string logName, int maxEvents = 200, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var eventLog = new System.Diagnostics.EventLog(logName);
            var entries = eventLog.Entries
                .Cast<System.Diagnostics.EventLogEntry>()
                .Where(e => e.EntryType is System.Diagnostics.EventLogEntryType.Error
                    or System.Diagnostics.EventLogEntryType.Warning)
                .OrderByDescending(e => e.TimeGenerated)
                .Take(maxEvents)
                .Select(entry =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var explanation = _errorAnalyzer.GetExplanation((int)entry.InstanceId, entry.Source);
                    var tag = _tagDetector.DetectTag(entry.Message, entry.Source);
                    var fullMessage = entry.Message ?? string.Empty;

                    return new EventItem
                    {
                        TimeCreated = entry.TimeGenerated.ToString("dd/MM/yyyy HH:mm"),
                        TimeCreatedAt = entry.TimeGenerated,
                        Level = GetLevelText(entry.EntryType),
                        Message = TruncateMessage(fullMessage),
                        FullMessage = fullMessage,
                        Source = entry.Source,
                        EventId = (int)entry.InstanceId,
                        ExplanationTitle = explanation.Title,
                        ExplanationDescription = explanation.Description,
                        ExplanationSolution = explanation.Solution,
                        ExplanationSeverity = explanation.Severity,
                        Tag = tag
                    };
                })
                .ToList();

            return (IReadOnlyList<EventItem>)entries;
        }, cancellationToken);
    }

    private static string GetLevelText(System.Diagnostics.EventLogEntryType entryType) => entryType switch
    {
        System.Diagnostics.EventLogEntryType.Error => "Erreur",
        System.Diagnostics.EventLogEntryType.Warning => "Avertissement",
        _ => "Information"
    };

    private static string TruncateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var cleaned = message.Replace("\r\n", " ").Replace('\n', ' ').Trim();
        return cleaned.Length <= 180 ? cleaned : cleaned[..177] + "...";
    }
}

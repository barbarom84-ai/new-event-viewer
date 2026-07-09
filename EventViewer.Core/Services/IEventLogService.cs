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
            var entries = eventLog.Entries;
            var count = entries.Count;
            if (count == 0)
            {
                return (IReadOnlyList<EventItem>)Array.Empty<EventItem>();
            }

            // Walk newest → oldest (Entries is chronological). Avoid OrderBy over the whole log.
            var results = new List<EventItem>(Math.Min(maxEvents, count));
            for (var i = count - 1; i >= 0 && results.Count < maxEvents; i--)
            {
                cancellationToken.ThrowIfCancellationRequested();

                System.Diagnostics.EventLogEntry entry;
                try
                {
                    entry = entries[i];
                }
                catch
                {
                    // Entry can disappear while iterating; skip.
                    continue;
                }

                if (entry.EntryType is not (System.Diagnostics.EventLogEntryType.Error
                    or System.Diagnostics.EventLogEntryType.Warning))
                {
                    continue;
                }

                var fullMessage = entry.Message ?? string.Empty;
                var eventId = (int)entry.InstanceId;
                var levelKind = EventSeverity.FromEntryType(entry.EntryType);
                var technicalDetail = EventDetailFormatter.FormatTechnicalDetail(eventId, entry.Source, fullMessage);
                var explanation = _errorAnalyzer.GetExplanation(eventId, entry.Source);
                var tag = _tagDetector.DetectTag(fullMessage, entry.Source);
                var component = ShouldResolveComponent(eventId, entry.Source, fullMessage)
                    ? ComComponentResolver.ResolveFromMessage(fullMessage, entry.Source)
                    : null;

                var listMessage = EventDetailFormatter.IsMissingDescriptionMessage(fullMessage)
                    ? BuildFriendlyListMessage(eventId, explanation.Title, component)
                    : TruncateMessage(fullMessage);

                results.Add(new EventItem
                {
                    TimeCreated = entry.TimeGenerated.ToString("g", System.Globalization.CultureInfo.CurrentUICulture),
                    TimeCreatedAt = entry.TimeGenerated,
                    Level = levelKind,
                    Message = listMessage,
                    FullMessage = technicalDetail,
                    Source = entry.Source,
                    EventId = eventId,
                    ExplanationTitle = explanation.Title,
                    ExplanationDescription = explanation.Description,
                    ExplanationSolution = explanation.Solution,
                    ExplanationSeverity = explanation.Severity,
                    RelatedComponentName = component?.DisplayName,
                    RelatedComponentGuid = component?.Guid,
                    RelatedComponentDisplay = component?.DisplayBoth,
                    Tag = tag
                });
            }

            return results;
        }, cancellationToken);
    }

    private static string BuildFriendlyListMessage(int eventId, string? title, ComComponentInfo? component)
    {
        var head = string.IsNullOrWhiteSpace(title) ? $"#{eventId}" : title;
        if (component != null && !string.IsNullOrWhiteSpace(component.DisplayName) &&
            !component.DisplayName.Equals("Composant COM inconnu", StringComparison.Ordinal))
        {
            return TruncateMessage($"{head} — {component.DisplayName}");
        }

        return TruncateMessage(head);
    }

    private static bool ShouldResolveComponent(int eventId, string source, string message)
        => eventId == 10016 || TagDetector.IsDcom(source, message);

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

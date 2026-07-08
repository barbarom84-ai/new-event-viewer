using CommunityToolkit.Mvvm.ComponentModel;
using EventViewer.Core;

namespace EventViewer.WinUI.ViewModels;

public partial class EventDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private string _headline = "Sélectionnez un incident";

    [ObservableProperty]
    private string _meaning = "Choisissez une ligne à gauche pour lire une explication simple.";

    [ObservableProperty]
    private string _action = "Rien à faire pour le moment.";

    [ObservableProperty]
    private string _technicalMessage = string.Empty;

    [ObservableProperty]
    private string _metaLine = string.Empty;

    [ObservableProperty]
    private string _assistantSummary = string.Empty;

    [ObservableProperty]
    private bool _hasAssistantSummary;

    [ObservableProperty]
    private string _feedbackMessage = string.Empty;

    [ObservableProperty]
    private bool _hasFeedbackMessage;

    [ObservableProperty]
    private bool _hasSelection;

    [ObservableProperty]
    private bool _showTechnical;

    [ObservableProperty]
    private string _autoFixTitle = string.Empty;

    [ObservableProperty]
    private string _autoFixExplanation = string.Empty;

    [ObservableProperty]
    private string _autoFixButtonLabel = "Corriger maintenant";

    [ObservableProperty]
    private bool _hasAutoFix;

    [ObservableProperty]
    private bool _canRunAutoFix;

    [ObservableProperty]
    private string _autoFixResult = string.Empty;

    [ObservableProperty]
    private bool _hasAutoFixResult;

    public AutoFixRecommendation? CurrentAutoFix { get; private set; }

    public void Clear()
    {
        HasSelection = false;
        Headline = "Sélectionnez un incident";
        Meaning = "Choisissez une ligne à gauche pour lire une explication simple.";
        Action = "Rien à faire pour le moment.";
        TechnicalMessage = string.Empty;
        MetaLine = string.Empty;
        AssistantSummary = string.Empty;
        HasAssistantSummary = false;
        FeedbackMessage = string.Empty;
        HasFeedbackMessage = false;
        ShowTechnical = false;
        ClearAutoFix();
    }

    public void Load(EventItem item, AutoFixRecommendation recommendation)
    {
        HasSelection = true;
        Headline = item.ExplanationTitle ?? "Incident Windows";
        Meaning = item.ExplanationDescription ?? item.Message;
        Action = item.ExplanationSolution ?? "Notez le message et redémarrez l'ordinateur si le problème continue.";
        TechnicalMessage = string.IsNullOrWhiteSpace(item.FullMessage) ? item.Message : item.FullMessage;
        MetaLine = $"{RelativeTimeFormatter.Format(item.TimeCreatedAt)} · Source : {item.Source} · Code {item.EventId}";
        ShowTechnical = false;
        AssistantSummary = string.Empty;
        HasAssistantSummary = false;
        FeedbackMessage = string.Empty;
        HasFeedbackMessage = false;
        ApplyAutoFix(recommendation);
    }

    public void ApplyAutoFix(AutoFixRecommendation recommendation)
    {
        CurrentAutoFix = recommendation;
        AutoFixTitle = recommendation.Title;
        AutoFixExplanation = recommendation.Explanation;
        AutoFixButtonLabel = recommendation.ButtonLabel;
        HasAutoFix = recommendation.Kind != AutoFixKind.None || !string.IsNullOrWhiteSpace(recommendation.Explanation);
        CanRunAutoFix = recommendation.CanExecuteAutomatically && recommendation.Kind != AutoFixKind.None;
        AutoFixResult = string.Empty;
        HasAutoFixResult = false;
    }

    public void ClearAutoFix()
    {
        CurrentAutoFix = null;
        AutoFixTitle = string.Empty;
        AutoFixExplanation = string.Empty;
        AutoFixButtonLabel = "Corriger maintenant";
        HasAutoFix = false;
        CanRunAutoFix = false;
        AutoFixResult = string.Empty;
        HasAutoFixResult = false;
    }

    public void ToggleTechnical() => ShowTechnical = !ShowTechnical;

    public void SetAssistantSummary(string summary)
    {
        AssistantSummary = summary;
        HasAssistantSummary = !string.IsNullOrWhiteSpace(summary);
    }

    public void SetFeedbackMessage(string message)
    {
        FeedbackMessage = message;
        HasFeedbackMessage = !string.IsNullOrWhiteSpace(message);
    }

    public void SetAutoFixResult(string message)
    {
        AutoFixResult = message;
        HasAutoFixResult = !string.IsNullOrWhiteSpace(message);
    }
}

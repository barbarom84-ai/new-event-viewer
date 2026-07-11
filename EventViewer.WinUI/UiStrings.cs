using CommunityToolkit.Mvvm.ComponentModel;
using EventViewer.Core;

namespace EventViewer.WinUI;

/// <summary>
/// Bindable UI strings that refresh when the language changes.
/// </summary>
public sealed class UiStrings : ObservableObject
{
    public static UiStrings Instance { get; } = new();

    private UiStrings()
    {
        Loc.LanguageChanged += () =>
        {
            OnPropertyChanged(string.Empty);
        };
    }

    public string AppTitle => Loc.T("App.Title");
    public string HealthBanner => Loc.T("Health.Banner");
    public string HealthErrors => Loc.T("Health.Errors");
    public string HealthWarnings => Loc.T("Health.Warnings");
    public string TimelineTitle => Loc.T("Timeline.Title");
    public string LogHeader => Loc.T("Log.Header");
    public string SearchHeader => Loc.T("Search.Header");
    public string SearchPlaceholder => Loc.T("Search.Placeholder");
    public string FilterSeverityHeader => Loc.T("Filter.Severity.Header");
    public string ActionRefresh => Loc.T("Action.Refresh");
    public string ActionExportCsv => Loc.T("Action.ExportCsv");
    public string ActionExportJson => Loc.T("Action.ExportJson");
    public string ActionOpenFolder => Loc.T("Action.OpenFolder");
    public string SnapshotTitle => Loc.T("Snapshot.Title");
    public string SnapshotSave => Loc.T("Snapshot.Save");
    public string SnapshotNew => Loc.T("Snapshot.New");
    public string SnapshotShowAll => Loc.T("Snapshot.ShowAll");
    public string OptionsTitle => Loc.T("Options.Title");
    public string OptionsTelemetry => Loc.T("Options.Telemetry");
    public string OptionsAiBeta => Loc.T("Options.AiBeta");
    public string OptionsAiConsent => Loc.T("Options.AiConsent");
    public string OptionsOn => Loc.T("Options.On");
    public string OptionsOff => Loc.T("Options.Off");
    public string OptionsAllowed => Loc.T("Options.Allowed");
    public string OptionsDenied => Loc.T("Options.Denied");
    public string OptionsAiEndpoint => Loc.T("Options.AiEndpoint");
    public string OptionsAiModel => Loc.T("Options.AiModel");
    public string OptionsAiKeyHint => Loc.T("Options.AiKeyHint");
    public string OptionsAdvancedMode => Loc.T("Options.AdvancedMode");
    public string OptionsAdvancedHint => Loc.T("Options.AdvancedHint");
    public string LangLabel => Loc.T("Lang.Label");
    public string LangHint => Loc.T("Lang.RestartHint");
    public string ThemeLabel => Loc.T("Theme.Label");
    public string ThemeHint => Loc.T("Theme.Hint");
    public string MaintenanceBusyTitle => Loc.T("Action.MaintenanceBusyTitle");
    public string MaintenanceTitle => Loc.T("Maintenance.Title");
    public string MaintenanceHint => Loc.T("Maintenance.Hint");
    public string MaintenanceFlushDns => Loc.T("Maintenance.FlushDns");
    public string MaintenanceSfc => Loc.T("Maintenance.Sfc");
    public string MaintenanceResetNetwork => Loc.T("Maintenance.ResetNetwork");
    public string AdminTitle => Loc.T("Admin.Title");
    public string AdminMessage => Loc.T("Admin.Message");
    public string DetailUnderstand => Loc.T("Detail.Understand");
    public string DetailWhatToDo => Loc.T("Detail.WhatToDo");
    public string DetailOneClick => Loc.T("Detail.OneClick");
    public string DetailFeedbackAsk => Loc.T("Detail.FeedbackAsk");
    public string DetailUseful => Loc.T("Detail.Useful");
    public string DetailNotUseful => Loc.T("Detail.NotUseful");
    public string DetailInBrief => Loc.T("Detail.InBrief");
    public string DetailMeaning => Loc.T("Detail.Meaning");
    public string DetailComponent => Loc.T("Detail.Component");
    public string DetailSummarize => Loc.T("Detail.Summarize");
    public string DetailShowTechnical => Loc.T("Detail.ShowTechnical");
    public string BadgeNew => Loc.T("Badge.New");
    public string AboutTitle => Loc.T("About.Title");
    public string ExportOpenFolder => Loc.T("Export.OpenFolder");
    public string MenuFile => Loc.T("Menu.File");
    public string MenuTools => Loc.T("Menu.Tools");
    public string MenuHelp => Loc.T("Menu.Help");
    public string MenuOptions => Loc.T("Menu.Options");
    public string DialogClose => Loc.T("Dialog.Close");
    public string DialogOk => Loc.T("Dialog.Ok");
}

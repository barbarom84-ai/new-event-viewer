using System.Reflection;
using EventViewer.Core;
using EventViewer.WinUI.Themes;
using EventViewer.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace EventViewer.WinUI.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        try
        {
            ViewModel = App.Services.GetRequiredService<MainViewModel>();
            InitializeComponent();
            DataContext = ViewModel;
            if (ToolsMenuItem != null)
            {
                ToolsMenuItem.Visibility = ViewModel.ShowMaintenance
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            App.WriteStartupCrash("MainPage ctor", ex);
            throw;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (ViewModel.LoadCommand.CanExecute(null))
        {
            ViewModel.LoadCommand.Execute(null);
        }
    }

    private void OnToggleTechnicalClick(object sender, RoutedEventArgs e)
        => ViewModel.ToggleTechnicalCommand.Execute(null);

    private async void OnAutoFixClick(object sender, RoutedEventArgs e)
    {
        var recommendation = ViewModel.Detail.CurrentAutoFix;
        if (recommendation == null || !recommendation.CanExecuteAutomatically)
        {
            return;
        }

        if (!await ConfirmAsync(recommendation.Title, recommendation.ConfirmMessage, Loc.T("Confirm.Primary.Fix")))
        {
            return;
        }

        await ViewModel.ExecuteConfirmedAutoFixAsync(recommendation);
    }

    private async void OnFlushDnsClick(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync(
                Loc.T("Confirm.Maintenance.FlushDns.Title"),
                Loc.T("Confirm.Maintenance.FlushDns.Message"),
                Loc.T("Confirm.Maintenance.FlushDns.Primary")))
        {
            return;
        }

        await ViewModel.ExecuteConfirmedFlushDnsAsync();
    }

    private async void OnRunSfcClick(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync(
                Loc.T("Confirm.Maintenance.Sfc.Title"),
                Loc.T("Confirm.Maintenance.Sfc.Message"),
                Loc.T("Confirm.Maintenance.Sfc.Primary")))
        {
            return;
        }

        await ViewModel.ExecuteConfirmedRunSfcAsync();
    }

    private async void OnResetNetworkClick(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmAsync(
                Loc.T("Confirm.Maintenance.ResetNetwork.Title"),
                Loc.T("Confirm.Maintenance.ResetNetwork.Message"),
                Loc.T("Confirm.Maintenance.ResetNetwork.Primary")))
        {
            return;
        }

        await ViewModel.ExecuteConfirmedResetNetworkAsync();
    }

    private async Task<bool> ConfirmAsync(string title, string message, string primary)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primary,
            CloseButtonText = Loc.T("Confirm.Cancel"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private async void OnOptionsClick(object sender, RoutedEventArgs e)
    {
        var content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 480,
            Content = BuildOptionsPanel()
        };

        var dialog = new ContentDialog
        {
            Title = Loc.T("Options.Title"),
            Content = content,
            CloseButtonText = Loc.T("Dialog.Close"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    private UIElement BuildOptionsPanel()
    {
        var muted = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppMutedBrush"];
        var root = new StackPanel { Spacing = 12, Width = 420, Padding = new Thickness(4, 0, 12, 0) };

        root.Children.Add(new TextBlock
        {
            Text = Loc.T("Lang.Label"),
            FontSize = 12,
            Foreground = muted
        });

        var languageBox = new ComboBox
        {
            ItemsSource = ViewModel.LanguageOptions,
            DisplayMemberPath = nameof(LanguageOption.DisplayName),
            SelectedItem = ViewModel.SelectedLanguage,
            Width = 260,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        languageBox.SelectionChanged += (_, _) =>
        {
            if (languageBox.SelectedItem is LanguageOption option)
            {
                ViewModel.SelectedLanguage = option;
            }
        };
        root.Children.Add(languageBox);
        root.Children.Add(new TextBlock
        {
            Text = Loc.T("Lang.RestartHint"),
            FontSize = 11,
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = muted
        });

        root.Children.Add(new TextBlock
        {
            Text = Loc.T("Theme.Label"),
            FontSize = 12,
            Margin = new Thickness(0, 8, 0, 0),
            Foreground = muted
        });

        var themeBox = new ComboBox
        {
            ItemsSource = ViewModel.ThemeOptions,
            DisplayMemberPath = nameof(ThemeOption.DisplayName),
            SelectedItem = ViewModel.SelectedTheme,
            Width = 260,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        themeBox.SelectionChanged += (_, _) =>
        {
            if (themeBox.SelectedItem is ThemeOption option)
            {
                ViewModel.SelectedTheme = option;
            }
        };
        root.Children.Add(themeBox);
        root.Children.Add(new TextBlock
        {
            Text = Loc.T("Theme.Hint"),
            FontSize = 11,
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = muted
        });

        var telemetry = new ToggleSwitch
        {
            Header = Loc.T("Options.Telemetry"),
            OnContent = Loc.T("Options.On"),
            OffContent = Loc.T("Options.Off"),
            IsOn = ViewModel.TelemetryOptIn
        };
        telemetry.Toggled += (_, _) => ViewModel.TelemetryOptIn = telemetry.IsOn;
        root.Children.Add(telemetry);

        var aiBeta = new ToggleSwitch
        {
            Header = Loc.T("Options.AiBeta"),
            OnContent = Loc.T("Options.On"),
            OffContent = Loc.T("Options.Off"),
            IsOn = ViewModel.AiBetaEnabled
        };
        root.Children.Add(aiBeta);

        var consent = new ToggleSwitch
        {
            Header = Loc.T("Options.AiConsent"),
            OnContent = Loc.T("Options.Allowed"),
            OffContent = Loc.T("Options.Denied"),
            IsOn = ViewModel.AiCloudDataConsent,
            IsEnabled = ViewModel.AiBetaEnabled
        };
        consent.Toggled += (_, _) => ViewModel.AiCloudDataConsent = consent.IsOn;
        aiBeta.Toggled += (_, _) =>
        {
            ViewModel.AiBetaEnabled = aiBeta.IsOn;
            consent.IsEnabled = aiBeta.IsOn;
        };
        root.Children.Add(consent);

        var endpoint = new TextBox
        {
            Header = Loc.T("Options.AiEndpoint"),
            PlaceholderText = "https://api.openai.com/v1/chat/completions",
            Text = ViewModel.AiEndpoint
        };
        endpoint.TextChanged += (_, _) => ViewModel.AiEndpoint = endpoint.Text;
        root.Children.Add(endpoint);

        var model = new TextBox
        {
            Header = Loc.T("Options.AiModel"),
            PlaceholderText = "gpt-4o-mini",
            Text = ViewModel.AiModel
        };
        model.TextChanged += (_, _) => ViewModel.AiModel = model.Text;
        root.Children.Add(model);

        root.Children.Add(new TextBlock
        {
            Text = ViewModel.AiCloudStatus,
            FontSize = 12,
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppMutedBrush"]
        });
        root.Children.Add(new TextBlock
        {
            Text = Loc.T("Options.AiKeyHint"),
            FontSize = 11,
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppMutedBrush"]
        });
        root.Children.Add(new TextBlock
        {
            Text = ViewModel.ToolsStatus,
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppMutedBrush"]
        });

        return root;
    }

    private async void OnAboutClick(object sender, RoutedEventArgs e)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.0";
        var year = DateTime.Now.Year.ToString();
        var storeNote = ViewModel.IsStoreBuild
            ? Loc.T("About.StoreEdition")
            : Loc.T("About.DesktopEdition");

        var dialog = new ContentDialog
        {
            Title = Loc.T("About.Title"),
            Content =
                $"{Loc.T("App.Title")} {version}\n" +
                $"{Loc.T("App.Description")}\n\n" +
                $"{Loc.T("About.Creator")}\n" +
                $"{Loc.T("About.Copyright", year)}\n\n" +
                $"{storeNote}",
            CloseButtonText = Loc.T("About.Close"),
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }
}

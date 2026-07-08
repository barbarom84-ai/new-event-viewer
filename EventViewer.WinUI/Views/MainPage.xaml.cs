using System.Reflection;
using EventViewer.Core;
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
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        InitializeComponent();
        DataContext = ViewModel;
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

        var dialog = new ContentDialog
        {
            Title = recommendation.Title,
            Content = recommendation.ConfirmMessage,
            PrimaryButtonText = "Corriger maintenant",
            CloseButtonText = "Annuler",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.ExecuteConfirmedAutoFixAsync(recommendation);
        }
    }

    private async void OnAboutClick(object sender, RoutedEventArgs e)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.0";
        var storeNote = ViewModel.IsStoreBuild
            ? "Édition Store (maintenance avancée masquée)."
            : "Édition USB / bureau (maintenance disponible si admin).";

        var dialog = new ContentDialog
        {
            Title = "À propos",
            Content =
                $"Observateur d'événements {version}\n" +
                "Comprenez les erreurs Windows en langage clair.\n\n" +
                $"{storeNote}\n\n" +
                "Télémétrie : opt-in, locale.\n" +
                "IA cloud : optionnelle (clé EVENTVIEWER_AI_API_KEY).\n\n" +
                "Éditeur : EventBeacon",
            CloseButtonText = "Fermer",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }
}

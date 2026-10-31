using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;

namespace BoardgameTracker.App.Games.ArkNova.Features.Settings;

public partial class ArkNovaSettingsPage : ContentPage
{
    private readonly ArkNovaSettingsViewModel _viewModel;

    public ArkNovaSettingsPage(ArkNovaSettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            _viewModel.Load();
    }

    protected override void OnDisappearing()
    {
        _viewModel.Save();
        base.OnDisappearing();
    }

    private async void OnMaterialInfoClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.GameMaterial, AppResources.GameMaterialHint, AppResources.Ok);

    private bool _openingCards;
    private async void OnSpecialCardsClicked(object? sender, EventArgs e)
    {
        if (_openingCards)
            return;
        _openingCards = true;
        try
        {
            await Shell.Current.GoToAsync(nameof(ArkNovaSpecialCardsPage));
        }
        finally
        {
            _openingCards = false;
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        _viewModel.Save();
        await Shell.Current.GoToAsync("..");
    }
}

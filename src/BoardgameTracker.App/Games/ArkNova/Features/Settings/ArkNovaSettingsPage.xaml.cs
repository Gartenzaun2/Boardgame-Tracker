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
        _viewModel.Load();
    }

    protected override void OnDisappearing()
    {
        _viewModel.Save();
        base.OnDisappearing();
    }

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

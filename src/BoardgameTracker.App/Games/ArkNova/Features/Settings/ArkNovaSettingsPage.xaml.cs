using BoardgameTracker.App.Features.Settings.MyGames;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Games.ArkNova.Features.Settings;

public partial class ArkNovaSettingsPage : ContentPage {
    private readonly ArkNovaSettingsViewModel _viewModel;
    private bool _choosing;
    private readonly MyGamesViewModel _visibility;

    public ArkNovaSettingsPage(ArkNovaSettingsViewModel viewModel, MyGamesViewModel visibility) {
        InitializeComponent();
        _viewModel = viewModel;
        _visibility = visibility;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing() {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this)) return;
        _viewModel.Load();
        VisibilityPanel.IsEnabled = false;
        await _visibility.LoadAsync();
        VisibilityPanel.BindingContext = _visibility.Games.SingleOrDefault(game => game.GameKey == ArkNovaGame.Key);
        VisibilityPanel.IsEnabled = !_visibility.HasError && VisibilityPanel.BindingContext is not null;
        VisibilityError.Text = _visibility.ErrorMessage;
        VisibilityError.IsVisible = _visibility.HasError;
    }

    private async void OnGameVisibilityToggled(object? sender, ToggledEventArgs e)
    {
        if (sender is not Switch { BindingContext: GameActivationRow row } || _visibility.IsBusy) return;
        VisibilityPanel.IsEnabled = false;
        await _visibility.SetEnabledAsync(row, e.Value);
        VisibilityPanel.IsEnabled = true;
        VisibilityError.Text = _visibility.ErrorMessage;
        VisibilityError.IsVisible = _visibility.HasError;
    }

    protected override void OnDisappearing() {
        _viewModel.Save();
        base.OnDisappearing();
    }

    private bool _openingCards;
    private async void OnPhotoDatasetClicked(object? sender, EventArgs e)
    {
        if (_choosing)
            return;
        _choosing = true;
        try { await Shell.Current.GoToAsync(nameof(ArkNovaPhotoDatasetPage), false); }
        finally { _choosing = false; }
    }

    private async void OnSpecialCardsClicked(object? sender, EventArgs e) {
        if(_openingCards)
            return;
        _openingCards = true;
        try {
            await Shell.Current.GoToAsync(nameof(ArkNovaSpecialCardsPage), false);
        } finally {
            _openingCards = false;
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e) {
        _viewModel.Save();
        await Shell.Current.GoToAsync("..", false);
    }

    private async void OnMaterialInfoClicked(object? sender, EventArgs e)
    {
        await AppDialogPage.ShowAsync(this, AppResources.GameMaterial, AppResources.GameMaterialHint, AppResources.Ok);
    }

    private async void OnContentLanguageClicked(object? sender, EventArgs e) {
        if(_choosing)
            return;

        _choosing = true;
        try {
            var options = _viewModel.ContentLanguages;

            int selected = Math.Max(
                0,
                options.ToList().FindIndex(
                    option => option.Code == _viewModel.SelectedContentLanguage?.Code));

            int choice = await AppDialogPage.ChooseAsync(
                this,
                AppResources.ContentLanguage,
                options.Select(option => option.Name).ToArray(),
                selected);

            if(choice < 0)
                return;

            _viewModel.SelectedContentLanguage = options[choice];
        } finally {
            _choosing = false;
        }
    }
}

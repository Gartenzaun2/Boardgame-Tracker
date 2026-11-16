using BoardgameTracker.App.Features.Settings.MyGames;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Games.ArkNova.Features.Settings;

public partial class ArkNovaSettingsPage : ContentPage {
    private readonly ArkNovaSettingsViewModel _viewModel;
    private bool _isChoosingOrNavigating;
    private readonly MyGamesViewModel _gamesSettingsViewModel;

    public ArkNovaSettingsPage(ArkNovaSettingsViewModel viewModel, MyGamesViewModel gamesSettingsViewModel) {
        InitializeComponent();
        _viewModel = viewModel;
        _gamesSettingsViewModel = gamesSettingsViewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing() {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;
        _viewModel.Load();
        EnablementPanel.IsEnabled = false;
        await _gamesSettingsViewModel.LoadAsync();
        EnablementPanel.BindingContext = _gamesSettingsViewModel.Games.SingleOrDefault(game => game.GameKey == ArkNovaGame.Key);
        EnablementPanel.IsEnabled = !_gamesSettingsViewModel.HasError && EnablementPanel.BindingContext is not null;
        EnablementError.Text = _gamesSettingsViewModel.ErrorMessage;
        EnablementError.IsVisible = _gamesSettingsViewModel.HasError;
    }

    private async void OnGameEnabledToggled(object? sender, ToggledEventArgs e) {
        if (sender is not Switch { BindingContext: GameSettingsRow row } || _gamesSettingsViewModel.IsBusy)
            return;
        EnablementPanel.IsEnabled = false;
        await _gamesSettingsViewModel.SetEnabledAsync(row, e.Value);
        EnablementPanel.IsEnabled = true;
        EnablementError.Text = _gamesSettingsViewModel.ErrorMessage;
        EnablementError.IsVisible = _gamesSettingsViewModel.HasError;
    }

    /// <summary>MAUI hides settings, including when opening a child page; persist the current choices before leaving.</summary>
    protected override void OnDisappearing() {
        _viewModel.Save();
        base.OnDisappearing();
    }

    private bool _openingCards;
    private async void OnPhotoDatasetClicked(object? sender, EventArgs e) {
        if (_isChoosingOrNavigating)
            return;
        _isChoosingOrNavigating = true;
        try {
            await Shell.Current.GoToAsync(nameof(ArkNovaPhotoDatasetPage), false);
        }
        finally {
            _isChoosingOrNavigating = false;
        }
    }

    private async void OnSpecialCardsClicked(object? sender, EventArgs e) {
        if (_openingCards)
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

    private async void OnCaptureReputationInfoClicked(object? sender, EventArgs e)
        => await AppDialogPage.ShowAsync(this, AppResources.CaptureReputation, AppResources.CaptureReputationHint, AppResources.Ok);

    private async void OnOldScoringInfoClicked(object? sender, EventArgs e)
        => await AppDialogPage.ShowAsync(this, AppResources.OldScoringDisplay, AppResources.OldScoringDisplayHint, AppResources.Ok);

    private async void OnMaterialInfoClicked(object? sender, EventArgs e) {
        await AppDialogPage.ShowAsync(this, AppResources.GameMaterial, AppResources.GameMaterialHint, AppResources.Ok);
    }

    private async void OnContentLanguageClicked(object? sender, EventArgs e) {
        if (_isChoosingOrNavigating)
            return;

        _isChoosingOrNavigating = true;
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

            if (choice < 0)
                return;

            _viewModel.SelectedContentLanguage = options[choice];
        } finally {
            _isChoosingOrNavigating = false;
        }
    }
}

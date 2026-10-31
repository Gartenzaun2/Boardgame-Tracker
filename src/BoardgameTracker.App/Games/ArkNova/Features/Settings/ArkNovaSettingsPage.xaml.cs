using BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Games.ArkNova.Features.Settings;

public partial class ArkNovaSettingsPage : ContentPage {
    private readonly ArkNovaSettingsViewModel _viewModel;
    private bool _choosing;

    public ArkNovaSettingsPage(ArkNovaSettingsViewModel viewModel) {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing() {
        base.OnAppearing();
        if(!AppDialogPage.IsOpen(this))
            _viewModel.Load();
    }

    protected override void OnDisappearing() {
        _viewModel.Save();
        base.OnDisappearing();
    }

    private bool _openingCards;
    private async void OnSpecialCardsClicked(object? sender, EventArgs e) {
        if(_openingCards)
            return;
        _openingCards = true;
        try {
            await Shell.Current.GoToAsync(nameof(ArkNovaSpecialCardsPage));
        } finally {
            _openingCards = false;
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e) {
        _viewModel.Save();
        await Shell.Current.GoToAsync("..");
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

using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Features.Games.ListGames;

public partial class GamesPage : ContentPage
{
    public GamesPage() => InitializeComponent();

    private async void OnInfoClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync(AppResources.Games, AppResources.GameCatalogHint, AppResources.Ok);
}

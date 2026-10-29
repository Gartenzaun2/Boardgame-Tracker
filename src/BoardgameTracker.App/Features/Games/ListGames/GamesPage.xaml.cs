using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.App.Features.Games.ListGames;

public partial class GamesPage : ContentPage
{
    private bool _isNavigating;

    public GamesPage()
    {
        InitializeComponent();
    }

    private async void OnInfoClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.Games, AppResources.GameCatalogHint, AppResources.Ok);

    private async void OnArkNovaTapped(object? sender, TappedEventArgs e)
    {
        if (_isNavigating)
        {
            return;
        }

        _isNavigating = true;
        try
        {
            if (Shell.Current is AppShell shell)
            {
                await shell.NavigateToGroupsForGameAsync(ArkNovaGame.Key);
            }
        }
        finally
        {
            _isNavigating = false;
        }
    }
}

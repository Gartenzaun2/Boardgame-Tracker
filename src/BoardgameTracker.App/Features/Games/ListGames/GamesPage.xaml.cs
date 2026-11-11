using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Features.Games.ListGames;

public partial class GamesPage : ContentPage
{
    private readonly GamesViewModel _viewModel;
    private bool _isNavigating;
    private readonly IGameCreationNavigation _creation;
    public GamesPage(GamesViewModel viewModel, IGameCreationNavigation creation)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _creation = creation;
        BindingContext = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await _viewModel.LoadAsync();
    }
    private async void OnCreateGameClicked(object? sender, EventArgs e)
    {
        if (_isNavigating || _viewModel.IsBusy)
            return;
        _isNavigating = true;
        try { await _creation.OpenCreateAsync(); }
        finally { _isNavigating = false; }
    }
    private async void OnInfoClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.Games, AppResources.GameCatalogHint, AppResources.Ok);
    private async void OnGameTapped(object? sender, TappedEventArgs e)
    {
        if (_isNavigating || _viewModel.IsBusy || e.Parameter is not string key
            || !_viewModel.Games.Any(game => game.Key == key && game.CanOpen))
            return;
        _isNavigating = true;
        IsEnabled = false;
        try
        {
            if (Shell.Current is AppShell shell)
                await shell.NavigateToGroupsForGameAsync(key);
        }
        catch (Exception)
        {
            await AppDialogPage.ShowAsync(this, AppResources.Games, AppResources.OpenGroupFailed, AppResources.Ok);
        }
        finally
        {
            _isNavigating = false; IsEnabled = true;
        }
    }
}

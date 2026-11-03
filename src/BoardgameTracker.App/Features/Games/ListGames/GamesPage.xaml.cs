using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Features.Games.ListGames;

public partial class GamesPage : ContentPage
{
    private readonly GamesViewModel _viewModel;
    private bool _isNavigating;
    public GamesPage(GamesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await _viewModel.LoadAsync();
    }
    private async void OnInfoClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.Games, AppResources.GameCatalogHint, AppResources.Ok);
    private async void OnGameTapped(object? sender, TappedEventArgs e)
    {
        if (_isNavigating || _viewModel.IsBusy || e.Parameter is not string key)
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

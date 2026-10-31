using BoardgameTracker.App.Features.Players.EditPlayer;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Players.ListPlayers;

public partial class PlayerListPage : ContentPage
{
    private readonly PlayerListViewModel _viewModel;
    private readonly ILogger<PlayerListPage> _logger;
    private bool _navigating;

    public PlayerListPage(PlayerListViewModel viewModel, ILogger<PlayerListPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await _viewModel.LoadAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (!_navigating && !_viewModel.IsBusy)
            await Shell.Current.GoToAsync("..");
    }
    private async void OnInfoClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.Players, AppResources.PlayersInfo, AppResources.Ok);

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (_navigating || _viewModel.IsBusy || sender is not ImageButton { CommandParameter: PlayerListRow row })
            return;
        _navigating = true;
        IsEnabled = false;
        try
        {
            await Shell.Current.GoToAsync(nameof(EditPlayerPage), new ShellNavigationQueryParameters { [NavigationParameters.PlayerId] = row.Data.PlayerId });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not open player editor.");
            await AppDialogPage.ShowAsync(this, AppResources.Players, AppResources.PlayerNavigationFailed, AppResources.Ok);
        }
        finally
        {
            _navigating = false;
            IsEnabled = true;
        }
    }

    private async void OnLastPlayedClicked(object? sender, EventArgs e)
    {
        if (!_navigating && sender is Button { CommandParameter: PlayerListRow row })
            await _viewModel.OpenLastGameSessionAsync(row);
    }

    protected override bool OnBackButtonPressed() => _navigating || _viewModel.IsBusy || base.OnBackButtonPressed();
}

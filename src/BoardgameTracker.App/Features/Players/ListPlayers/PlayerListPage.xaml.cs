using BoardgameTracker.App.Features.Players.EditPlayer;
using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;
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
        if (!AppDialogPage.IsOpen(this)) await _viewModel.LoadAsync();
    }
    public void SelectPlayers() => _viewModel.SelectPlayers();
    private void OnPlayersSegmentClicked(object? sender, EventArgs e) => _viewModel.SelectPlayers();
    private void OnGroupsSegmentClicked(object? sender, EventArgs e) => _viewModel.SelectGroups();
    private async void OnInfoClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.Participants, AppResources.PlayersInfo, AppResources.Ok);
    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: PlayerListRow row })
            await NavigateAsync(nameof(EditPlayerPage), new() { [NavigationParameters.PlayerId] = row.Data.PlayerId });
    }
    private async void OnAddPlayerClicked(object? sender, EventArgs e) =>
        await NavigateAsync(nameof(EditPlayerPage), new() { [NavigationParameters.NewPlayer] = true });
    private async void OnAddGroupClicked(object? sender, EventArgs e) =>
        await NavigateAsync(nameof(CreateGroupPage), new() { [NavigationParameters.GroupManagement] = true });
    private async void OnEditGroupClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ManagedGroupRow row })
            await NavigateAsync(nameof(EditGroupPage), new() { [NavigationParameters.GroupId] = row.Id });
    }
    private async Task NavigateAsync(string route, ShellNavigationQueryParameters parameters)
    {
        if (_navigating || _viewModel.IsBusy) return;
        _navigating = true;
        IsEnabled = false;
        try { await Shell.Current.GoToAsync(route, false, parameters); }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not open management editor.");
            await AppDialogPage.ShowAsync(this, AppResources.Participants, AppResources.PlayerNavigationFailed, AppResources.Ok);
        }
        finally { _navigating = false; IsEnabled = true; }
    }
    protected override bool OnBackButtonPressed() => _navigating || _viewModel.IsBusy || base.OnBackButtonPressed();
}

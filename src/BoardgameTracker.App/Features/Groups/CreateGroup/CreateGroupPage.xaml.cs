using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.Groups.CreateGroup;

/// <summary>
/// MAUI view for group creation; visual dialogs and concrete Shell navigation remain view responsibilities.
/// </summary>
public partial class CreateGroupPage : ContentPage, IQueryAttributable
{
    private readonly CreateGroupViewModel _viewModel;
    private readonly IEnumerable<IGameSessionNavigationHandler> _handlers;
    public CreateGroupViewModel ViewModel => _viewModel;

    public CreateGroupPage(CreateGroupViewModel viewModel, IEnumerable<IGameSessionNavigationHandler> handlers)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _handlers = handlers;
        _viewModel.ShowDuplicatesAsync = () => Shell.Current.GoToAsync(nameof(DuplicateGroupPage), false, new ShellNavigationQueryParameters { [NavigationParameters.GroupDraft] = _viewModel });
        _viewModel.SavedAsync = OnSavedAsync;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationParameters.GroupManagement, out object? value) && value is true)
            _viewModel.SetManagementOrigin();
        else if (query.TryGetValue(NavigationParameters.GameKey, out object? key) && key is string gameKey)
            _viewModel.SetGameContext(gameKey);
    }

    private async Task OnSavedAsync(GroupSaveOutcome outcome)
    {
        if (outcome.GameKey is null)
        {
            await Shell.Current.GoToAsync(outcome.FromDuplicates ? "../.." : "..", false);
            return;
        }
        IGameSessionNavigationHandler handler = _handlers.Single(handler => handler.GameKey == outcome.GameKey);
        await handler.OpenGroupAsync(outcome.GroupId, null, null);
        // Only remove completed form pages after the handler successfully opened preparation.
        var navigation = Shell.Current.CurrentItem.CurrentItem.Navigation;
        foreach (Page page in navigation.NavigationStack.Where(page => ReferenceEquals(page, this)
            || page is DuplicateGroupPage duplicate && ReferenceEquals(duplicate.ViewModel, _viewModel)).ToArray())
            navigation.RemovePage(page);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;
        await _viewModel.LoadAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.IsBusy)
            await Shell.Current.GoToAsync("..", false);
    }

    private async void OnInfoClicked(object? sender, EventArgs e)
    {
        await AppDialogPage.ShowAsync(this,
            AppResources.CreateGroup,
            AppResources.CreateGroupHint,
            AppResources.Ok);
    }

    private async void OnPlayersInfoClicked(object? sender, EventArgs e)
    {
        await AppDialogPage.ShowAsync(this,
            AppResources.GroupPlayers,
            AppResources.DefaultOrderHint,
            AppResources.Ok);
    }

    private void OnNewPlayerClicked(object? sender, EventArgs e) => _viewModel.BeginNewPlayer();
    private void OnCancelNewPlayerClicked(object? sender, EventArgs e) => _viewModel.CancelNewPlayer();

    private void OnChooseExistingPlayerClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsBusy || !_viewModel.HasAvailablePlayers)
            return;
        ExistingPlayerOverlay.IsVisible = true;
    }
    private void OnCloseExistingPlayerClicked(object? sender, EventArgs e) => ExistingPlayerOverlay.IsVisible = false;
    private void OnExistingPlayerTapped(object? sender, TappedEventArgs e)
    {
        if (!ExistingPlayerOverlay.IsVisible || _viewModel.IsBusy || e.Parameter is not SelectablePlayer player
            || !_viewModel.AvailablePlayers.Contains(player)) return;
        ExistingPlayerOverlay.IsVisible = false;
        _viewModel.SelectedExistingPlayer = player;
        if (_viewModel.AddSelectedPlayerCommand.CanExecute(null)) _viewModel.AddSelectedPlayerCommand.Execute(null);
    }
    private void OnChooseGameClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.IsBusy) _viewModel.IsChoosingGame = true;
    }
    private void OnCloseGameClicked(object? sender, EventArgs e) => _viewModel.IsChoosingGame = false;
    private void OnGameTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel.IsChoosingGame && e.Parameter is GroupGameSelection game)
            _viewModel.AddGameCommand.Execute(game);
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsBusy)
            return true;
        if (_viewModel.IsChoosingGame) { _viewModel.IsChoosingGame = false; return true; }
        if (_viewModel.IsAddingNewPlayer) { _viewModel.CancelNewPlayer(); return true; }
        if (!ExistingPlayerOverlay.IsVisible) return base.OnBackButtonPressed();
        ExistingPlayerOverlay.IsVisible = false;
        return true;
    }

}

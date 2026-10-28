using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Groups.CreateGroup;

namespace BoardgameTracker.App.Features.Groups.CreateGroup;

/// <summary>
/// MAUI view for group creation; visual dialogs and concrete Shell navigation remain view responsibilities.
/// </summary>
public partial class CreateGroupPage : ContentPage
{
    private readonly CreateGroupViewModel _viewModel;

    public CreateGroupPage(CreateGroupViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _viewModel.Saved += OnSaved;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnInfoClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync(
            AppResources.CreateGroup,
            AppResources.CreateGroupHint,
            AppResources.Ok);
    }

    private async void OnPlayersInfoClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync(
            AppResources.GroupPlayers,
            AppResources.DefaultOrderHint,
            AppResources.Ok);
    }

    private void OnChooseExistingPlayerClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsBusy || !_viewModel.HasAvailablePlayers) return;
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
    protected override bool OnBackButtonPressed()
    {
        if (!ExistingPlayerOverlay.IsVisible) return base.OnBackButtonPressed();
        ExistingPlayerOverlay.IsVisible = false;
        return true;
    }

    private async void OnSaved(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

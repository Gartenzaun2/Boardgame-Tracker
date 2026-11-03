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
    public CreateGroupViewModel ViewModel => _viewModel;

    public CreateGroupPage(CreateGroupViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _viewModel.ShowDuplicatesAsync = () => Shell.Current.GoToAsync(nameof(DuplicateGroupPage),
            new ShellNavigationQueryParameters { [NavigationParameters.GroupDraft] = _viewModel });
        _viewModel.SavedAsync = fromDuplicates => Shell.Current.GoToAsync(fromDuplicates ? "../.." : "..");
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationParameters.GroupManagement, out object? value) && value is true)
            _viewModel.SetManagementOrigin();
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
            await Shell.Current.GoToAsync("..");
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
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsBusy)
            return true;
        if (!ExistingPlayerOverlay.IsVisible) return base.OnBackButtonPressed();
        ExistingPlayerOverlay.IsVisible = false;
        return true;
    }

}

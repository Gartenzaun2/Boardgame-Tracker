using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.Groups.EditGroup;

/// <summary>
/// MAUI view for editing group metadata, order and game assignments.
/// </summary>
public partial class EditGroupPage : ContentPage, IQueryAttributable
{
    private readonly EditGroupViewModel _viewModel;
    public EditGroupViewModel ViewModel => _viewModel;
    private Guid _groupId;

    public EditGroupPage(EditGroupViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _viewModel.SavedAsync = OnSavedAsync;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GroupId, out object? value)
            || value is not Guid groupId
            || groupId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Required navigation parameter '{NavigationParameters.GroupId}' is missing or invalid.");
        }

        _groupId = groupId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;

        if (_groupId != Guid.Empty)
        {
            await _viewModel.LoadAsync(_groupId);
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsBusy)
            return;
        if (_viewModel.IsSaved)
            await _viewModel.SaveCommand.ExecuteAsync(null);
        else
            await Shell.Current.GoToAsync("..", false);
    }

    private async void OnInfoClicked(object? sender, EventArgs e)
    {
        await AppDialogPage.ShowAsync(this,
            AppResources.EditGroup,
            AppResources.EditGroupHint + "\n\n" + AppResources.ArchiveHint,
            AppResources.Ok);
    }

    private async void OnPlayersInfoClicked(object? sender, EventArgs e)
    {
        await AppDialogPage.ShowAsync(this,
            AppResources.GroupPlayers,
            AppResources.EditGroupPlayersHint,
            AppResources.Ok);
    }

    private void OnChooseGameClicked(object? sender, EventArgs e)
    {
        if (_viewModel.CanEdit) _viewModel.IsChoosingGame = true;
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
        if (_viewModel.IsSaved)
        {
            _viewModel.SaveCommand.Execute(null);
            return true;
        }
        if (_viewModel.IsChoosingGame) { _viewModel.IsChoosingGame = false; return true; }
        return base.OnBackButtonPressed();
    }

    private Task OnSavedAsync() => Shell.Current.GoToAsync("..", false);
}

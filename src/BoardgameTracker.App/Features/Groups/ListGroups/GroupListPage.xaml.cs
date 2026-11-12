using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

public partial class GroupListPage : ContentPage, IQueryAttributable
{
    private readonly GroupListViewModel _viewModel;
    private readonly ILogger<GroupListPage> _logger;
    public string GameKey => _viewModel.SelectedGameKey;
    public GroupListPage(GroupListViewModel viewModel, ILogger<GroupListPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GameKey, out object? value) || value is not string key || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("A game context is required.");
        _viewModel.SetGameContext(key);
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;
        _viewModel.EndNavigation();
        await _viewModel.LoadAsync();
    }
    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanInteract)
            return;
        await Shell.Current.GoToAsync("..", false);
    }
    private async void OnCreateGroupClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanCreate || !_viewModel.TryBeginNavigation())
            return;
        try
        {
            await Shell.Current.GoToAsync(nameof(CreateGroupPage), false, new ShellNavigationQueryParameters { [NavigationParameters.GameKey] = _viewModel.SelectedGameKey });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not open group creation.");
            await AppDialogPage.ShowAsync(this, AppResources.Groups, AppResources.NavigationFailed, AppResources.Ok);
        }
        finally
        {
            _viewModel.EndNavigation();
        }
    }
    private async void OnGroupTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Guid id)
            await _viewModel.OpenGroupAsync(id);
    }
    private void OnChooseExistingClicked(object? sender, EventArgs e)
    {
        if (_viewModel.CanChoose && _viewModel.CanInteract) _viewModel.IsChoosingExisting = true;
    }
    private void OnCloseSelectionClicked(object? sender, EventArgs e) => _viewModel.IsChoosingExisting = false;
    private async void OnExistingTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is AvailablePlayGroup group)
            await _viewModel.SelectExistingAsync(group);
    }
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsBusy || _viewModel.IsNavigating)
            return true;
        if (!_viewModel.IsChoosingExisting) return base.OnBackButtonPressed();
        _viewModel.IsChoosingExisting = false;
        return true;
    }
}

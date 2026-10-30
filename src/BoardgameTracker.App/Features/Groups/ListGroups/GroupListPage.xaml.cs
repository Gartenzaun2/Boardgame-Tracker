using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

/// <summary>
/// Owns group selection and form navigation; registered game handlers own game routes.
/// Navigation re-entry state remains in the view model.
/// </summary>
public partial class GroupListPage : ContentPage
{
    private readonly GroupListViewModel _viewModel;
    private readonly IEnumerable<IGameSessionNavigationHandler> _navigationHandlers;

    public GroupListPage(GroupListViewModel viewModel, IEnumerable<IGameSessionNavigationHandler> navigationHandlers)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _navigationHandlers = navigationHandlers;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.EndNavigation();
        await _viewModel.LoadAsync();
    }

    private async void OnCreateGroupClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.TryBeginNavigation())
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync(nameof(CreateGroupPage));
        }
        catch
        {
            _viewModel.EndNavigation();
            throw;
        }
    }

    private async void OnGroupTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Guid groupId || !_viewModel.TryBeginNavigation())
        {
            return;
        }

        GroupListItemViewModel? group = _viewModel.Groups.FirstOrDefault(candidate => candidate.Id == groupId);

        if (group?.IsArchived == true)
        {
            _viewModel.EndNavigation();
            return;
        }

        IGameSessionNavigationHandler? handler = _navigationHandlers.FirstOrDefault(candidate => candidate.GameKey == _viewModel.SelectedGameKey);
        if (handler is null)
        {
            _viewModel.EndNavigation();
            return;
        }

        try
        {
            await handler.OpenGroupAsync(groupId, group?.OpenGameSessionId, group?.OpenGameSessionStatus);
        }
        catch
        {
            _viewModel.EndNavigation();
            throw;
        }
    }

    private async void OnEditGroupClicked(object? sender, EventArgs e)
    {
        if (sender is not ImageButton { CommandParameter: Guid groupId }
            || !_viewModel.TryBeginNavigation())
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync(
                nameof(EditGroupPage),
                new ShellNavigationQueryParameters { [NavigationParameters.GroupId] = groupId });
        }
        catch
        {
            _viewModel.EndNavigation();
            throw;
        }
    }
}

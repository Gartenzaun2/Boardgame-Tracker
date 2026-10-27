using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.GameSessions.Preparation;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

public partial class GroupListPage : ContentPage
{
    private readonly GroupListViewModel _viewModel;

    public GroupListPage(GroupListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
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

        try
        {
            await Shell.Current.GoToAsync(
                nameof(ArkNovaPreparationPage),
                new Dictionary<string, object> { ["GroupId"] = groupId });
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
                new Dictionary<string, object> { ["GroupId"] = groupId });
        }
        catch
        {
            _viewModel.EndNavigation();
            throw;
        }
    }

}

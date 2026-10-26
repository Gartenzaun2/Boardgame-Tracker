using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;

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
        await _viewModel.LoadAsync();
    }

    private async void OnCreateGroupClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CreateGroupPage));
    }

    private async void OnEditGroupClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Guid groupId })
        {
            return;
        }

        await Shell.Current.GoToAsync(
            nameof(EditGroupPage),
            new Dictionary<string, object>
            {
                ["GroupId"] = groupId
            });
    }
}

using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;

namespace BoardgameTracker.App.Features.Groups.ListGroups;

public partial class GroupListPage : ContentPage
{
    private readonly GroupListViewModel _viewModel;
    private bool _isNavigating;

    public GroupListPage(GroupListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SetNavigationBusy(false);
        await _viewModel.LoadAsync();
    }

    private async void OnCreateGroupClicked(object? sender, EventArgs e)
    {
        if (!TryBeginNavigation())
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync(nameof(CreateGroupPage));
        }
        catch
        {
            SetNavigationBusy(false);
            throw;
        }
    }

    private async void OnEditGroupClicked(object? sender, EventArgs e)
    {
        if (sender is not ImageButton { CommandParameter: Guid groupId }
            || !TryBeginNavigation())
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
            SetNavigationBusy(false);
            throw;
        }
    }

    private bool TryBeginNavigation()
    {
        if (_isNavigating)
        {
            return false;
        }

        SetNavigationBusy(true);
        return true;
    }

    private void SetNavigationBusy(bool isBusy)
    {
        _isNavigating = isBusy;
        NavigationBusyOverlay.IsVisible = isBusy;
        NavigationBusyIndicator.IsRunning = isBusy;
    }
}

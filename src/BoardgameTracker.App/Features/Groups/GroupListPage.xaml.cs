namespace BoardgameTracker.App.Features.Groups;

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
}

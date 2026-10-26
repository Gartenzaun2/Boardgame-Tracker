namespace BoardgameTracker.App.Features.Groups;

public partial class GroupListPage : ContentPage
{
    public GroupListPage(GroupListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

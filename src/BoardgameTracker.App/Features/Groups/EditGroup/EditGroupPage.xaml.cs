using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Features.Groups.EditGroup;

public partial class EditGroupPage : ContentPage, IQueryAttributable
{
    private readonly EditGroupViewModel _viewModel;
    private Guid _groupId;

    public EditGroupPage(EditGroupViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _viewModel.Saved += OnSaved;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("GroupId", out object? value)
            && value is Guid groupId)
        {
            _groupId = groupId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_groupId != Guid.Empty)
        {
            await _viewModel.LoadAsync(_groupId);
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnInfoClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync(
            AppResources.EditGroup,
            AppResources.EditGroupHint,
            AppResources.Ok);
    }

    private async void OnMembersInfoClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync(
            AppResources.GroupMembers,
            AppResources.EditGroupMembersHint,
            AppResources.Ok);
    }

    private async void OnSaved(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

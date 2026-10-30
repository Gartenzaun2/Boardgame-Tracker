using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.Groups.EditGroup;

/// <summary>
/// MAUI view for editing group metadata, order and Ark Nova default colors.
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
        _viewModel.Saved += OnSaved;
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
        await Shell.Current.GoToAsync("..");
    }

    private async void OnInfoClicked(object? sender, EventArgs e)
    {
        await AppDialogPage.ShowAsync(this,
            AppResources.EditGroup,
            AppResources.EditGroupHint,
            AppResources.Ok);
    }

    private async void OnPlayersInfoClicked(object? sender, EventArgs e)
    {
        await AppDialogPage.ShowAsync(this,
            AppResources.GroupPlayers,
            AppResources.EditGroupPlayersHint,
            AppResources.Ok);
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsBusy)
            return true;
        if (!_viewModel.ColorSelection.IsVisible) return base.OnBackButtonPressed();
        _viewModel.ColorSelection.Close();
        return true;
    }

    private async void OnSaved(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

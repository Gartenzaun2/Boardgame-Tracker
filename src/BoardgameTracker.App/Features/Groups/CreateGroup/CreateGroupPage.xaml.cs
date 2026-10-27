using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Features.Groups.CreateGroup;

/// <summary>
/// MAUI view for group creation; visual dialogs and concrete Shell navigation remain view responsibilities.
/// </summary>
public partial class CreateGroupPage : ContentPage
{
    private readonly CreateGroupViewModel _viewModel;

    public CreateGroupPage(CreateGroupViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _viewModel.Saved += OnSaved;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnInfoClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync(
            AppResources.CreateGroup,
            AppResources.CreateGroupHint,
            AppResources.Ok);
    }

    private async void OnPlayersInfoClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync(
            AppResources.GroupPlayers,
            AppResources.DefaultOrderHint,
            AppResources.Ok);
    }

    private async void OnSaved(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

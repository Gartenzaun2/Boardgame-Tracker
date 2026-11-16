using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.Groups.CreateGroup;

public partial class CreateGroupPage : ContentPage, IQueryAttributable {
    private readonly CreateGroupViewModel _viewModel;
    public CreateGroupViewModel ViewModel => _viewModel;

    public CreateGroupPage(CreateGroupViewModel viewModel) {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) {
        if (query.TryGetValue(NavigationParameters.GroupManagement, out object? value) && value is true)
            _viewModel.SetManagementOrigin();
        else if (query.TryGetValue(NavigationParameters.GameKey, out object? key) && key is string gameKey)
            _viewModel.SetGameContext(gameKey);
    }

    /// <summary>MAUI shows or resumes the page; ignore its own modal dismissal so loading cannot replace in-progress form state.</summary>
    protected override async void OnAppearing() {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;
        await _viewModel.LoadAsync();
    }

    private async void OnInfoClicked(object? sender, EventArgs e) {
        await AppDialogPage.ShowAsync(this,
            AppResources.CreateGroup,
            AppResources.CreateGroupHint,
            AppResources.Ok);
    }

    private async void OnPlayersInfoClicked(object? sender, EventArgs e) {
        await AppDialogPage.ShowAsync(this,
            AppResources.GroupPlayers,
            AppResources.DefaultOrderHint,
            AppResources.Ok);
    }

    /// <summary>Routes system back through the same overlay cancellation and departure command as the header.</summary>
    protected override bool OnBackButtonPressed() {
        _viewModel.GoBackCommand.Execute(null);
        return true;
    }
}

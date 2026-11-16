using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;

namespace BoardgameTracker.App.Features.Groups.EditGroup;

public partial class EditGroupPage : ContentPage, IQueryAttributable {
    private readonly EditGroupViewModel _viewModel;
    private Guid _groupId;

    public EditGroupPage(EditGroupViewModel viewModel) {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public EditGroupViewModel ViewModel => _viewModel;

    public void ApplyQueryAttributes(IDictionary<string, object> query) {
        if (!query.TryGetValue(NavigationParameters.GroupId, out object? value) || value is not Guid groupId || groupId == Guid.Empty)
            throw new InvalidOperationException($"Required navigation parameter '{NavigationParameters.GroupId}' is missing or invalid.");

        _groupId = groupId;
    }

    /// <summary>MAUI shows or resumes the page; ignore its own modal dismissal so loading cannot replace in-progress form state.</summary>
    protected override async void OnAppearing() {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;

        if (_groupId != Guid.Empty)
            await _viewModel.LoadAsync(_groupId);
    }

    private async void OnInfoClicked(object? sender, EventArgs e) {
        await AppDialogPage.ShowAsync(this,
            AppResources.EditGroup,
            AppResources.EditGroupHint + "\n\n" + AppResources.ArchiveHint,
            AppResources.Ok);
    }

    private async void OnPlayersInfoClicked(object? sender, EventArgs e) {
        await AppDialogPage.ShowAsync(this,
            AppResources.GroupPlayers,
            AppResources.EditGroupPlayersHint,
            AppResources.Ok);
    }

    /// <summary>Routes system back through the header command, including cancellation, busy guards and navigation errors.</summary>
    protected override bool OnBackButtonPressed() {
        _viewModel.GoBackCommand.Execute(null);
        return true;
    }
}

using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;

namespace BoardgameTracker.App.Features.Players.EditPlayer;

public partial class EditPlayerPage : ContentPage, IQueryAttributable {
    private readonly EditPlayerViewModel _viewModel;
    private Guid _playerId;

    public EditPlayerPage(EditPlayerViewModel viewModel) {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) {
        if (query.TryGetValue(NavigationParameters.NewPlayer, out object? create) && create is true) {
            _viewModel.InitializeNew();
            return;
        }
        if (!query.TryGetValue(NavigationParameters.PlayerId, out object? value) || value is not Guid playerId || playerId == Guid.Empty)
            throw new InvalidOperationException("Required player navigation parameter is missing or invalid.");
        _playerId = playerId;
    }

    /// <summary>Returning from a modal must not replace edits in the open player form.</summary>
    protected override async void OnAppearing() {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this) && !_viewModel.IsNew)
            await _viewModel.LoadAsync(_playerId);
    }

    /// <summary>Protects the active workflow and retries departure after a completed save.</summary>
    protected override bool OnBackButtonPressed() {
        if (_viewModel.IsBusy)
            return true;
        if (_viewModel.IsSaveCompleted) {
            // MAUI requires a synchronous result; the command owns async navigation and errors.
            _viewModel.GoBackCommand.Execute(null);
            return true;
        }
        return base.OnBackButtonPressed();
    }
}

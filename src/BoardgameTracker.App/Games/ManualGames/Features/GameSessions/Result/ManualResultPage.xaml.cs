using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Result;

public partial class ManualResultPage : ContentPage, IQueryAttributable {
    private readonly ManualResultViewModel _viewModel;
    private readonly ManualGameSessionNavigationHandler _navigation;
    private readonly ILogger<ManualResultPage> _logger;
    private Guid _id;
    private bool _isNavigating;
    public ManualResultPage(ManualResultViewModel viewModel, ManualGameSessionNavigationHandler navigation, ILogger<ManualResultPage> logger) {
        InitializeComponent();
        _viewModel = viewModel;
        _navigation = navigation;
        _logger = logger;
        BindingContext = viewModel;
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query) {
        if (query.TryGetValue(NavigationParameters.GameSessionId, out object? value) && value is Guid id)
            _id = id;
    }
    protected override async void OnAppearing() {
        base.OnAppearing();
        await _viewModel.LoadAsync(_id);
    }
    private async void OnEditClicked(object? sender, EventArgs e) {
        if (_isNavigating || _viewModel.IsBusy || !_viewModel.CanCorrect)
            return;
        _isNavigating = true;
        try {
            await _navigation.OpenCorrectionAsync(_id);
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Could not open historical manual correction.");
            await AppDialogPage.ShowAsync(this, AppResources.EditResult, AppResources.ScoringLoadFailed, AppResources.Ok);
        }
        finally {
            _isNavigating = false;
        }
    }

    private async Task ReturnAsync() {
        if (_isNavigating || _viewModel.IsBusy)
            return;
        _isNavigating = true;
        try {
            await ((AppShell)Shell.Current).ReturnToGameSessionCallerAsync();
        }
        finally {
            _isNavigating = false;
        }
    }
    private async void OnDoneClicked(object? sender, EventArgs e)
        => await ReturnAsync();
    /// <summary>Routes system back to the retained caller so completed session pages do not remain in the back stack.</summary>
    protected override bool OnBackButtonPressed() {
        // The synchronous back callback dispatches the asynchronous close/navigation flow.
        Dispatcher.Dispatch(async () => await ReturnAsync());
        return true;
    }
}

using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.Maui.Dispatching;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Running;

public partial class ManualRunningPage : ContentPage, IQueryAttributable
{
    private readonly ManualRunningViewModel _viewModel;
    private readonly ManualGameSessionNavigationHandler _navigation;
    private readonly IDispatcherTimer _durationTimer;
    private readonly ILogger<ManualRunningPage> _logger;
    private Guid _sessionId;
    private bool _returning;
    public ManualRunningPage(ManualRunningViewModel viewModel, ManualGameSessionNavigationHandler navigation, ILogger<ManualRunningPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _navigation = navigation;
        _logger = logger;
        BindingContext = viewModel;
        _durationTimer = Dispatcher.CreateTimer();
        _durationTimer.Interval = TimeSpan.FromSeconds(30);
        _durationTimer.Tick += (_, _) => _viewModel.RefreshDuration(DateTimeOffset.UtcNow);
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationParameters.GameSessionId, out object? value) && value is Guid id)
            _sessionId = id;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!AppDialogPage.IsOpen(this))
            await _viewModel.LoadAsync(_sessionId);
        _durationTimer.Start();
    }
    protected override void OnDisappearing()
    {
        _durationTimer.Stop();
        base.OnDisappearing();
    }
    private async Task ReturnAsync()
    {
        if (_returning || !_viewModel.CanInteract)
            return;
        _returning = true;
        try { await ((AppShell)Shell.Current).ReturnToGameSessionCallerAsync(); }
        finally { _returning = false; }
    }
    private async void OnBackClicked(object? sender, EventArgs e) => await ReturnAsync();
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.CanInteract && !_returning)
            Dispatcher.Dispatch(async () => await ReturnAsync());
        return true;
    }
    private async void OnEndClicked(object? sender, EventArgs e)
    {
        if (_returning || !_viewModel.CanInteract)
            return;
        _returning = true;
        try
        {
            if (await _viewModel.BeginScoringAsync())
                await _navigation.ContinueAsync(_viewModel.SessionId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not open manual scoring.");
            await AppDialogPage.ShowAsync(this, AppResources.Scoring, AppResources.ScoringLoadFailed, AppResources.Ok);
        }
        finally
        {
            _returning = false;
        }
    }
    private async void OnDiscardClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.CanInteract || _returning)
            return;
        bool confirmed;
        _viewModel.IsConfirming = true;
        try
        {
            confirmed = await AppDialogPage.ShowAsync(this, AppResources.DiscardGameTitle, AppResources.DiscardGameMessage,
                AppResources.Discard, AppResources.Cancel, destructive: true);
        }
        finally { _viewModel.IsConfirming = false; }
        if (await _viewModel.DiscardAsync(confirmed))
            await ReturnAsync();
    }
}

using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Navigation;
using Microsoft.Maui.Dispatching;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;

/// <summary>
/// MAUI view for an active Ark Nova gameSession. It owns the visual duration timer and
/// delegates lifecycle actions to the running GameSession view model.
/// </summary>
public partial class ArkNovaRunningPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaRunningViewModel _viewModel;
    private readonly IDispatcherTimer _durationTimer;
    private readonly ILogger<ArkNovaRunningPage> _logger;
    private bool _isNavigating;
    private bool _hasBegunScoring;
    private bool _isDiscarded;

    public ArkNovaRunningPage(ArkNovaRunningViewModel viewModel, ILogger<ArkNovaRunningPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = _viewModel;

        _durationTimer = Dispatcher.CreateTimer();
        _durationTimer.Interval = TimeSpan.FromSeconds(30);
        _durationTimer.Tick += (_, _) => _viewModel.RefreshDuration(DateTimeOffset.UtcNow);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_gameSessionId != Guid.Empty && _viewModel.GameSessionId != _gameSessionId)
        {
            await _viewModel.LoadAsync(_gameSessionId);
        }

        _viewModel.RefreshDuration(DateTimeOffset.UtcNow);
        _durationTimer.Start();
    }

    protected override void OnDisappearing()
    {
        _durationTimer.Stop();
        base.OnDisappearing();
    }


    private async void OnBackClicked(object? sender, EventArgs e) =>
        await ReturnAsync();

    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () => await ReturnAsync());
        return true;
    }

    private async Task ReturnAsync()
    {
        if (_isNavigating || _viewModel.IsBusy)
            return;
        _isNavigating = true;
        try
        {
            await ((AppShell)Shell.Current).ReturnToGameSessionCallerAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not return from the running Ark Nova session.");
            await ShowNavigationErrorAsync(_isDiscarded ? AppResources.NavigationAfterDiscardFailed : AppResources.NavigationFailed);
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private async void OnEndClicked(object? sender, EventArgs e)
    {
        if (_isNavigating || _viewModel.IsBusy || _isDiscarded)
            return;
        _isNavigating = true;
        try
        {
            if (!_hasBegunScoring)
            {
                if (!await _viewModel.BeginScoringAsync())
                    return;
                _hasBegunScoring = true;
            }

            await Shell.Current.GoToAsync(nameof(ArkNovaScoringPage), false, new ShellNavigationQueryParameters
                {
                    [NavigationParameters.GameSessionId] = _viewModel.GameSessionId
                });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not open Ark Nova scoring. Persistence completed: {Persisted}.", _hasBegunScoring);
            await ShowNavigationErrorAsync(_hasBegunScoring ? AppResources.NavigationAfterScoringStartedFailed : AppResources.EndGameFailed);
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private async void OnDiscardClicked(object? sender, EventArgs e)
    {
        if (_isNavigating || _viewModel.IsBusy || _hasBegunScoring)
            return;
        _isNavigating = true;
        try
        {
            if (!_isDiscarded)
            {
                bool confirmed = await AppDialogPage.ShowAsync(this,
                    AppResources.DiscardGameTitle,
                    AppResources.DiscardGameMessage,
                    AppResources.Discard,
                    AppResources.Cancel, destructive: true);

                if (!confirmed || !await _viewModel.DiscardAsync())
                    return;
                _isDiscarded = true;
            }

            await ((AppShell)Shell.Current).ReturnToGameSessionCallerAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not leave discarded Ark Nova session. Persistence completed: {Persisted}.", _isDiscarded);
            await ShowNavigationErrorAsync(_isDiscarded ? AppResources.NavigationAfterDiscardFailed : AppResources.DiscardGameFailed);
        }
        finally
        {
            _isNavigating = false;
        }
    }

    private async Task ShowNavigationErrorAsync(string message)
    {
        try
        {
            await AppDialogPage.ShowAsync(this, AppResources.ArkNova, message, AppResources.Ok);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not display the Ark Nova running navigation error.");
        }
    }
}

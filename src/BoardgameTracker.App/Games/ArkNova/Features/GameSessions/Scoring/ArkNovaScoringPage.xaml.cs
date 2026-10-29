using BoardgameTracker.App.Presentation.Controls;
using Microsoft.Extensions.Logging;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;
using BoardgameTracker.App.Games.ArkNova;
using BoardgameTracker.App.Games.ArkNova.Features.Settings;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaScoringPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaScoringViewModel _viewModel;
    private readonly ILogger<ArkNovaScoringPage> _logger;
    private Guid _gameSessionId;

    public ArkNovaScoringPage(ArkNovaScoringViewModel viewModel, ILogger<ArkNovaScoringPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GameSessionId, out object? value)
            || value is not Guid gameSessionId || gameSessionId == Guid.Empty)
        {
            throw new InvalidOperationException($"Required navigation parameter '{NavigationParameters.GameSessionId}' is missing or invalid.");
        }

        _gameSessionId = gameSessionId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (AppDialogPage.IsOpen(this))
            return;
        if (_gameSessionId != Guid.Empty)
        {
            await _viewModel.LoadAsync(_gameSessionId);
        }
    }

    private async void OnPlayerTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ArkNovaScoringPlayerViewModel player || !_viewModel.TryBeginNavigation())
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync(
                nameof(ArkNovaPlayerScoringPage),
                new ShellNavigationQueryParameters
                {
                    [NavigationParameters.GameSessionId] = _gameSessionId,
                    [NavigationParameters.PlayerId] = player.PlayerId,
                    [NavigationParameters.CorrectionDraft] = _viewModel.Draft!
                });
        }
        finally
        {
            _viewModel.EndNavigation();
        }
    }

    private void OnAddSpecialCardTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is Guid playerId)
        {
            _viewModel.OpenSpecialCardPicker(playerId);
        }
    }

    private async void OnSpecialCardOptionTapped(object? sender, TappedEventArgs e)
    {
        if (_saving) return;
        _saving = true;
        try
        {
            if (e.Parameter is not ArkNovaSpecialCardOptionViewModel option
            || _viewModel.SpecialCardTargetPlayerId is not Guid playerId)
            {
                return;
            }

            if (option.IsAssignedToTarget)
            {
                await _viewModel.RemoveSpecialCardAsync(playerId, option.Card.Id);
                return;
            }

            if (option.IsAssignedToOtherPlayer)
            {
                bool confirmed = await AppDialogPage.ShowAsync(this,
                    AppResources.SpecialCardMultipleTitle,
                    string.Format(
                        AppResources.SpecialCardMultipleMessageFormat,
                        option.Card.Name),
                    AppResources.Yes,
                    AppResources.Cancel);

                if (!confirmed)
                {
                    return;
                }
            }

            await _viewModel.AssignSpecialCardAsync(playerId, option.Card.Id);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Ark Nova scoring operation failed for GameSession {GameSessionId}.", _gameSessionId);
            await AppDialogPage.ShowAsync(this, AppResources.Scoring, AppResources.CorrectionFailed, AppResources.Ok);
        }
        finally { _saving = false; }
    }

    private void OnSpecialCardPickerBackClicked(object? sender, EventArgs e) =>
        _viewModel.CloseSpecialCardPicker();

    private bool _saving;
    private async void OnCompleteClicked(object? sender, EventArgs e)
    {
        if (_saving) return;
        _saving = true;
        try
        {
            ArkNovaCompletionPlan? plan = await _viewModel.PrepareCompletionAsync();
            if (plan is null) return;

            if (_viewModel.IsHistorical && !plan.HasTies)
            {
                await _viewModel.CompleteWithoutTieAsync(plan.DefaultPlacements);
                return;
            }
            string route = plan.HasTies ? nameof(ArkNovaTieResolutionPage) : nameof(ArkNovaResultPage);
            if (!plan.HasTies)
            {
                await _viewModel.CompleteWithoutTieAsync(plan.DefaultPlacements);
            }

            await Shell.Current.GoToAsync(
                route,
                new ShellNavigationQueryParameters { [NavigationParameters.GameSessionId] = _gameSessionId, [NavigationParameters.CorrectionDraft] = _viewModel.Draft! });
        }

        catch (Exception exception)
        {
            _logger.LogError(exception, "Ark Nova scoring operation failed for GameSession {GameSessionId}.", _gameSessionId);
            await AppDialogPage.ShowAsync(this, AppResources.Scoring, AppResources.CorrectionFailed, AppResources.Ok);
        }
        finally { _saving = false; }
    }

    private async void OnDiscardClicked(object? sender, EventArgs e)
    {
        if (_saving) return;
        _saving = true;
        try
        {
            bool confirmed = await AppDialogPage.ShowAsync(this,
            _viewModel.IsHistorical ? AppResources.DeleteGameSessionTitle : AppResources.DiscardScoringTitle,
            _viewModel.IsHistorical ? AppResources.DeleteGameSessionMessage : AppResources.DiscardScoringMessage,
            _viewModel.IsHistorical ? AppResources.DeleteGameSession : AppResources.Discard,
            AppResources.Cancel, destructive: true);

            if (confirmed && await _viewModel.DiscardAsync())
            {
                await Shell.Current.GoToAsync(_viewModel.IsHistorical ? "//HistoryPage" : $"//{nameof(GroupListPage)}");
            }

        }
        finally { _saving = false; }
    }

    private async void OnCameraClicked(object? sender, EventArgs e) =>
        await AppDialogPage.ShowAsync(this, AppResources.PhotoRecognition, AppResources.PhotoRecognitionPending, AppResources.Ok);

    private async void OnSettingsClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ArkNovaSettingsPage));

    private async void OnBackClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(_viewModel.IsHistorical ? "//HistoryPage" : $"//{nameof(GroupListPage)}");

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsSpecialCardPickerVisible)
        {
            _viewModel.CloseSpecialCardPicker();
            return true;
        }

        Dispatcher.Dispatch(async () => await Shell.Current.GoToAsync(_viewModel.IsHistorical ? "//HistoryPage" : $"//{nameof(GroupListPage)}"));
        return true;
    }
}

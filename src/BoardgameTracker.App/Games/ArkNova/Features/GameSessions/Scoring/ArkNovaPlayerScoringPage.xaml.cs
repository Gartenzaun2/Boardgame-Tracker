using Microsoft.Extensions.Logging;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaPlayerScoringPage : ContentPage, IQueryAttributable
{
    private readonly ArkNovaPlayerScoringViewModel _viewModel;
    private readonly ILogger<ArkNovaPlayerScoringPage> _logger;
    private Guid _gameSessionId;
    private Guid _playerId;

    public ArkNovaPlayerScoringPage(ArkNovaPlayerScoringViewModel viewModel, ILogger<ArkNovaPlayerScoringPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationParameters.GameSessionId, out object? gameSessionValue)
            || gameSessionValue is not Guid gameSessionId || gameSessionId == Guid.Empty
            || !query.TryGetValue(NavigationParameters.PlayerId, out object? playerValue)
            || playerValue is not Guid playerId || playerId == Guid.Empty)
        {
            throw new InvalidOperationException("Required scoring navigation parameters are missing or invalid.");
        }
        _viewModel.Draft = query.TryGetValue(NavigationParameters.CorrectionDraft, out var draft) ? draft as BoardgameTracker.Application.Games.ArkNova.GameSessions.History.ArkNovaCorrectionDraft : null;
        _gameSessionId = gameSessionId;
        _playerId = playerId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(_gameSessionId, _playerId);
    }

    private bool _saving;
    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        if (_saving) return;
        _saving = true;
        try
        {
            if (!_viewModel.IsValid) return;
            await _viewModel.FlushAsync();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not save scoring for GameSession {GameSessionId}, Player {PlayerId}.", _gameSessionId, _playerId);
            await DisplayAlertAsync(BoardgameTracker.App.Resources.Localization.AppResources.Scoring,
            BoardgameTracker.App.Resources.Localization.AppResources.CorrectionFailed, BoardgameTracker.App.Resources.Localization.AppResources.Ok);
        }
        finally { _saving = false; }
    }
}

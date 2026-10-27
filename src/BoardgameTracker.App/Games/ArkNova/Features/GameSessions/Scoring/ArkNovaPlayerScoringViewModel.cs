using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.Settings;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaPlayerScoringViewModel(
    ArkNovaScoringUseCase scoringUseCase,
    IArkNovaSettingsStore settingsStore) : ObservableObject
{
    private CancellationTokenSource? _autosaveCts;
    private bool _loading;

    [ObservableProperty] public partial Guid GameSessionId { get; private set; }
    [ObservableProperty] public partial Guid PlayerId { get; private set; }
    [ObservableProperty] public partial string Name { get; private set; } = string.Empty;
    [ObservableProperty] public partial string AttractivenessText { get; set; } = string.Empty;
    [ObservableProperty] public partial string ConservationText { get; set; } = string.Empty;
    [ObservableProperty] public partial string ReputationText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsReputationVisible { get; private set; }
    [ObservableProperty] public partial string ScoreText { get; private set; } = "–";
    [ObservableProperty] public partial string? ValidationMessage { get; private set; }

    private ArkNovaScoringDisplayMode _displayMode;

    public async Task LoadAsync(Guid gameSessionId, Guid playerId, CancellationToken cancellationToken = default)
    {
        _loading = true;
        try
        {
            ArkNovaScoringGameSessionData? gameSession = await scoringUseCase.LoadAsync(gameSessionId, cancellationToken);
            ArkNovaScoringPlayerData? player = gameSession?.Players.SingleOrDefault(x => x.PlayerId == playerId);
            if (player is null) throw new InvalidOperationException("The scoring player does not exist.");

            ArkNovaSettings settings = settingsStore.Get();
            _displayMode = settings.ScoringDisplayMode;
            GameSessionId = gameSessionId;
            PlayerId = playerId;
            Name = player.Name;
            AttractivenessText = player.Attractiveness?.ToString() ?? string.Empty;
            ConservationText = player.Conservation?.ToString() ?? string.Empty;
            ReputationText = player.Reputation?.ToString() ?? string.Empty;
            IsReputationVisible = settings.CaptureReputation;
            RefreshCalculatedScore();
        }
        finally { _loading = false; }
    }

    partial void OnAttractivenessTextChanged(string value) => ScheduleAutosave();
    partial void OnConservationTextChanged(string value) => ScheduleAutosave();
    partial void OnReputationTextChanged(string value) => ScheduleAutosave();

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        _autosaveCts?.Cancel();
        if (TryReadValues(out int? attractiveness, out int? conservation, out int? reputation))
        {
            await scoringUseCase.SavePlayerScoreAsync(GameSessionId, PlayerId, attractiveness, conservation, reputation, cancellationToken);
        }
    }

    private void ScheduleAutosave()
    {
        if (_loading || GameSessionId == Guid.Empty) return;
        RefreshCalculatedScore();
        _autosaveCts?.Cancel();
        CancellationTokenSource cts = new();
        _autosaveCts = cts;
        _ = AutosaveAfterDelayAsync(cts);
    }

    private async Task AutosaveAfterDelayAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(450, cts.Token);
            if (!TryReadValues(out int? attractiveness, out int? conservation, out int? reputation)) return;
            await scoringUseCase.SavePlayerScoreAsync(GameSessionId, PlayerId, attractiveness, conservation, reputation, cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
        finally
        {
            if (ReferenceEquals(_autosaveCts, cts)) _autosaveCts = null;
            cts.Dispose();
        }
    }

    private bool TryReadValues(out int? attractiveness, out int? conservation, out int? reputation)
    {
        ValidationMessage = null;
        attractiveness = ParseOptional(AttractivenessText);
        conservation = ParseOptional(ConservationText);
        reputation = IsReputationVisible ? ParseOptional(ReputationText) : null;

        if ((!string.IsNullOrWhiteSpace(AttractivenessText) && attractiveness is null)
            || attractiveness is < 0 or > 113)
        {
            ValidationMessage = "Attraktivität muss zwischen 0 und 113 liegen.";
            return false;
        }
        if ((!string.IsNullOrWhiteSpace(ConservationText) && conservation is null)
            || conservation is < 0 or > 41)
        {
            ValidationMessage = "Artenschutz muss zwischen 0 und 41 liegen.";
            return false;
        }
        if (IsReputationVisible
            && ((!string.IsNullOrWhiteSpace(ReputationText) && reputation is null)
                || reputation is < 1 or > 15))
        {
            ValidationMessage = "Ruf muss leer bleiben oder zwischen 1 und 15 liegen.";
            return false;
        }
        return true;
    }

    private void RefreshCalculatedScore()
    {
        int? attractiveness = ParseOptional(AttractivenessText);
        int? conservation = ParseOptional(ConservationText);
        if (attractiveness is >= 0 and <= 113 && conservation is >= 0 and <= 41)
        {
            int score = _displayMode == ArkNovaScoringDisplayMode.Old
                ? ArkNovaScoringRules.CalculateOldScore(attractiveness.Value, conservation.Value)
                : ArkNovaScoringRules.CalculateNewScore(attractiveness.Value, conservation.Value);
            ScoreText = score.ToString();
        }
        else
        {
            ScoreText = "–";
        }
    }

    private static int? ParseOptional(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : int.TryParse(text, out int value) ? value : null;
}

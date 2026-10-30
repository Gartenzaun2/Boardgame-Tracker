using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.App.Presentation.Controls;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.Settings;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Players;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaPlayerScoringViewModel(
    ArkNovaScoringUseCase scoringUseCase,
    IArkNovaSettingsStore settingsStore,
    ILogger<ArkNovaPlayerScoringViewModel> logger) : ObservableObject
{
    // MAUI Slider bounds require double values, including when supplied through x:Static.
    public const double ReputationMinimum = ArkNovaScoringRules.MinimumReputation;
    public const double ReputationMaximum = ArkNovaScoringRules.MaximumReputation;

    public ArkNovaHistoryCorrectionDraft? Draft { get; set; }
    private CancellationTokenSource? _autosaveCts;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private bool _loading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }
    public bool IsIdle => !IsBusy;

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
            ArkNovaGameSessionResultData? gameSession = Draft?.Data ?? await scoringUseCase.LoadGameSessionResultAsync(gameSessionId, cancellationToken);
            ArkNovaGameSessionResultPlayerData? player = gameSession?.Players.SingleOrDefault(x => x.PlayerId == playerId);
            if (player is null)
            {
                throw new InvalidOperationException("The scoring player does not exist.");
            }

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
        finally
        {
            _loading = false;
        }
    }

    public int? AttractivenessValue
    {
        get => ParseWheelValue(AttractivenessText, AttractivenessRange);
        set => AttractivenessText = value?.ToString() ?? string.Empty;
    }
    public int? ConservationValue
    {
        get => ParseWheelValue(ConservationText, ConservationRange);
        set => ConservationText = value?.ToString() ?? string.Empty;
    }
    public int? ReputationValue
    {
        get => ParseWheelValue(ReputationText, ReputationRange);
        set => ReputationText = value?.ToString() ?? string.Empty;
    }
    public bool IsAttractivenessSuggested => string.IsNullOrWhiteSpace(AttractivenessText);
    public bool IsConservationSuggested => string.IsNullOrWhiteSpace(ConservationText);
    public bool IsReputationSuggested => string.IsNullOrWhiteSpace(ReputationText);
    public double ReputationSliderValue
    {
        get => ReputationRange.DisplayedValue(ReputationValue);
        set => ReputationValue = (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
    public string ScoreTitle => IsAttractivenessSuggested || IsConservationSuggested
        ? AppResources.ScorePreview : AppResources.CalculatedPoints;

    private static readonly NumberWheelRange AttractivenessRange = new(ArkNovaScoringRules.MinimumAttractiveness, ArkNovaScoringRules.MaximumAttractiveness);
    private static readonly NumberWheelRange ConservationRange = new(ArkNovaScoringRules.MinimumConservation, ArkNovaScoringRules.MaximumConservation);
    private static readonly NumberWheelRange ReputationRange = new(ArkNovaScoringRules.MinimumReputation, ArkNovaScoringRules.MaximumReputation);

    public string AttractivenessRangeText => $"{AttractivenessRange.Minimum}–{AttractivenessRange.Maximum}";
    public string ConservationRangeText => $"{ConservationRange.Minimum}–{ConservationRange.Maximum}";
    public string ReputationRangeText => $"{ReputationRange.Minimum}–{ReputationRange.Maximum}";

    public string AttractivenessSuggestion => AttractivenessRange.DisplayedValue(null).ToString();
    public string ConservationSuggestion => ConservationRange.DisplayedValue(null).ToString();
    public string ReputationSuggestion => ReputationRange.DisplayedValue(null).ToString();

    public void ConfirmSuggestedValues()
    {
        if (IsAttractivenessSuggested)
            AttractivenessValue = AttractivenessRange.DisplayedValue(null);
        if (IsConservationSuggested)
            ConservationValue = ConservationRange.DisplayedValue(null);
    }

    partial void OnAttractivenessTextChanged(string value)
    {
        OnPropertyChanged(nameof(AttractivenessValue));
        OnPropertyChanged(nameof(IsAttractivenessSuggested));
        OnPropertyChanged(nameof(ScoreTitle));
        ScheduleAutosave();
    }
    partial void OnConservationTextChanged(string value)
    {
        OnPropertyChanged(nameof(ConservationValue));
        OnPropertyChanged(nameof(IsConservationSuggested));
        OnPropertyChanged(nameof(ScoreTitle));
        ScheduleAutosave();
    }
    partial void OnReputationTextChanged(string value)
    {
        OnPropertyChanged(nameof(ReputationValue));
        OnPropertyChanged(nameof(ReputationSliderValue));
        OnPropertyChanged(nameof(IsReputationSuggested));
        ScheduleAutosave();
    }

    public bool IsValid => TryReadValues(out _, out _, out _);
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        _autosaveCts?.Cancel();
        IsBusy = true;
        try
        {
            await SaveValuesAsync(cancellationToken, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveValuesAsync(CancellationToken waitToken, CancellationToken saveToken)
    {
        await _saveGate.WaitAsync(waitToken);
        try
        {
            if (!TryReadValues(out int? attractiveness, out int? conservation, out int? reputation))
                return;

            if (Draft is not null)
            {
                Draft.SetScore(PlayerId, attractiveness, conservation, reputation);
            }
            else
            {
                await scoringUseCase.SavePlayerScoreAsync(GameSessionId, PlayerId, attractiveness, conservation, reputation, saveToken);
            }
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private void ScheduleAutosave()
    {
        if (_loading || GameSessionId == Guid.Empty)
        {
            return;
        }
        RefreshCalculatedScore();
        if (Draft is not null)
        {
            return;
        }
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
            // Cancel queued/debounced work, but let an active write finish before the final flush.
            await SaveValuesAsync(cts.Token, CancellationToken.None);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not autosave Ark Nova player scoring.");
            ValidationMessage = AppResources.CorrectionFailed;
        }
        finally
        {
            if (ReferenceEquals(_autosaveCts, cts))
            {
                _autosaveCts = null;
            }
            cts.Dispose();
        }
    }

    private bool TryReadValues(out int? attractiveness, out int? conservation, out int? reputation)
    {
        ValidationMessage = null;
        attractiveness = ParseOptional(AttractivenessText);
        conservation = ParseOptional(ConservationText);
        reputation = IsReputationVisible || Draft is not null ? ParseOptional(ReputationText) : null;

        if ((!string.IsNullOrWhiteSpace(AttractivenessText) && attractiveness is null)
            || attractiveness is < 0 or > 113)
        {
            ValidationMessage = AppResources.AttractivenessValidation;
            return false;
        }
        if ((!string.IsNullOrWhiteSpace(ConservationText) && conservation is null)
            || conservation is < 0 or > 41)
        {
            ValidationMessage = AppResources.ConservationValidation;
            return false;
        }
        if (IsReputationVisible
            && ((!string.IsNullOrWhiteSpace(ReputationText) && reputation is null)
                || reputation is < 1 or > 15))
        {
            ValidationMessage = AppResources.ReputationValidation;
            return false;
        }
        if (Draft is not null && (attractiveness is null || conservation is null))
        {
            ValidationMessage = AppResources.CorrectionInvalid;
            return false;
        }
        return true;
    }

    private void RefreshCalculatedScore()
    {
        int? attractiveness = IsAttractivenessSuggested ? AttractivenessRange.DisplayedValue(null) : AttractivenessValue;
        int? conservation = IsConservationSuggested ? ConservationRange.DisplayedValue(null) : ConservationValue;
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

    private static int? ParseWheelValue(string text, NumberWheelRange range)
    {
        int? value = ParseOptional(text);
        return value >= range.Minimum && value <= range.Maximum ? value : null;
    }

    private static int? ParseOptional(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : int.TryParse(text, out int value) ? value : null;
}

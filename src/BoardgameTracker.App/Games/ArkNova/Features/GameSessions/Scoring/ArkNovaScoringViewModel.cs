using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.App.Presentation.PlayerColors;
using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Games.ArkNova;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.Settings;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaScoringViewModel(
    ArkNovaScoringUseCase scoringUseCase,
    ArkNovaHistoryCorrectionUseCase correction,
    IArkNovaSettingsStore settingsStore,
    ILogger<ArkNovaScoringViewModel> logger) : ObservableObject
{
    private IReadOnlyList<ArkNovaSpecialCardData> _cardCatalog = [];
    public ObservableCollection<ArkNovaScoringPlayerViewModel> Players { get; } = [];
    public ObservableCollection<ArkNovaSpecialCardOptionViewModel> SpecialCardOptions { get; } = [];

    [ObservableProperty] public partial Guid GameSessionId { get; private set; }
    [ObservableProperty] public partial string GroupName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string DateText { get; private set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsScoringContentEnabled))]
    [NotifyPropertyChangedFor(nameof(CanComplete))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsScoringContentEnabled))]
    [NotifyPropertyChangedFor(nameof(CanComplete))]
    public partial bool IsSaving { get; set; }
    public bool IsIdle => !IsBusy && !IsSaving;
    public bool IsScoringContentEnabled => IsIdle && !IsSpecialCardPickerVisible;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanComplete))]
    public partial bool CompletionSaved { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScoringContentEnabled))]
    [NotifyPropertyChangedFor(nameof(CanComplete))]
    public partial bool IsSpecialCardPickerVisible { get; private set; }
    [ObservableProperty] public partial Guid? SpecialCardTargetPlayerId { get; private set; }

    public ArkNovaHistoryCorrectionDraft? Draft { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHistorical))]
    [NotifyPropertyChangedFor(nameof(IsLiveScoring))]
    [NotifyPropertyChangedFor(nameof(CompleteText))]
    [NotifyPropertyChangedFor(nameof(DeleteText))]
    public partial ArkNovaScoringMode Mode { get; private set; }
    public bool IsHistorical => Mode == ArkNovaScoringMode.HistoryCorrection;
    public bool IsLiveScoring => !IsHistorical;
    public string CompleteText => IsHistorical ? AppResources.SaveCorrection : AppResources.CompleteScoring;
    public string DeleteText => IsHistorical ? AppResources.DeleteGameSession : AppResources.Discard;
    public bool CanComplete => IsScoringContentEnabled && !CompletionSaved && Players.Count > 0 && Players.All(x => x.HasRequiredValues);

    public string SpecialCardPickerTitle
    {
        get
        {
            ArkNovaScoringPlayerViewModel? player = SpecialCardTargetPlayerId is Guid playerId
                ? Players.FirstOrDefault(x => x.PlayerId == playerId)
                : null;

            return player is null
                ? AppResources.SpecialCards
                : string.Format(AppResources.SpecialCardPickerTitleFormat, player.Name);
        }
    }

    public async Task LoadAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            ArkNovaGameSessionResultData? data = await scoringUseCase.LoadGameSessionResultAsync(gameSessionId, cancellationToken);
            if (data is null)
            {
                ErrorMessage = AppResources.ScoringNotFound;
                return;
            }

            if (data.Status == GameSessionStatus.Completed)
            {
                if (Draft?.Saved == true)
                {
                    Draft = null;
                }
                Draft ??= await correction.LoadCorrectionDraftAsync(gameSessionId, cancellationToken);
                Draft.RefreshCardCatalog(data.CardCatalog);
                data = Draft.Data with
                {
                    Players = Draft.Data.Players.OrderBy(p => p.Placement).ThenBy(p => p.Order).ToArray(),
                    ZooPlanNames = data.ZooPlanNames
                };
            }
            Mode = data.Status == GameSessionStatus.Completed ? ArkNovaScoringMode.HistoryCorrection : ArkNovaScoringMode.Live;
            ArkNovaSettings settings = settingsStore.Get();
            GameSessionId = data.GameSessionId;
            CompletionSaved = false;
            GroupName = data.GroupName;
            DateText = data.StartedAt.ToLocalTime().ToString("d", CultureInfo.CurrentCulture);
            _cardCatalog = data.CardCatalog;
            Players.Clear();
            foreach (ArkNovaGameSessionResultPlayerData player in data.Players)
            {
                Guid[] cards = data.SpecialCards
                    .Where(x => x.PlayerId == player.PlayerId)
                    .Select(x => x.SpecialCardId)
                    .ToArray();
                Players.Add(new ArkNovaScoringPlayerViewModel(
                    player,
                    cards,
                    settings.ScoringDisplayMode,
                    settings.CaptureReputation, data.ZooPlanNames[player.ZooPlanNumber]));
            }

            OnPropertyChanged(nameof(CanComplete));
            RefreshSpecialCardOptions();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load Ark Nova scoring GameSession {GameSessionId}.", gameSessionId);
            ErrorMessage = UserErrorMessage.Create(AppResources.ScoringLoadFailed, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OpenSpecialCardPicker(Guid playerId)
    {
        if (!IsScoringContentEnabled || Players.All(x => x.PlayerId != playerId))
        {
            return;
        }

        SpecialCardTargetPlayerId = playerId;
        IsSpecialCardPickerVisible = true;
        OnPropertyChanged(nameof(SpecialCardPickerTitle));
        RefreshSpecialCardOptions();
    }

    public void CloseSpecialCardPicker()
    {
        IsSpecialCardPickerVisible = false;
        SpecialCardTargetPlayerId = null;
        SpecialCardOptions.Clear();
        OnPropertyChanged(nameof(SpecialCardPickerTitle));
    }

    public async Task AssignSpecialCardAsync(
        Guid playerId,
        Guid card,
        CancellationToken cancellationToken = default)
    {
        CloseSpecialCardPicker();
        if (IsHistorical)
        {
            Draft!.SetCard(playerId, card, true);
        }
        else
        {
            await scoringUseCase.AssignSpecialCardAsync(GameSessionId, playerId, card, cancellationToken);
        }
        await LoadAsync(GameSessionId, cancellationToken);
    }

    public async Task RemoveSpecialCardAsync(
        Guid playerId,
        Guid card,
        CancellationToken cancellationToken = default)
    {
        CloseSpecialCardPicker();
        if (IsHistorical)
        {
            Draft!.SetCard(playerId, card, false);
        }
        else
        {
            await scoringUseCase.RemoveSpecialCardAsync(GameSessionId, playerId, card, cancellationToken);
        }
        await LoadAsync(GameSessionId, cancellationToken);
    }

    public bool TryBeginNavigation()
    {
        if (!IsScoringContentEnabled)
        {
            return false;
        }

        IsBusy = true;
        return true;
    }

    public void EndNavigation() => IsBusy = false;

    public bool IsSpecialCardAssignedToOtherPlayer(Guid playerId, Guid card) =>
        Players.Any(x => x.PlayerId != playerId && x.SpecialCards.Contains(card));

    public async Task<ArkNovaGameSessionCompletionPlan?> PrepareCompletionAsync(CancellationToken cancellationToken = default)
    {
        if (Players.Count == 0 || Players.Any(x => !x.HasRequiredValues))
        {
            return null;
        }
        return IsHistorical ? correction.CreateCorrectionCompletionPlan(Draft!) : await scoringUseCase.PrepareCompletionAsync(GameSessionId, cancellationToken);
    }

    public async Task CompleteWithoutTieAsync(
        IReadOnlyDictionary<Guid, int> placements,
        CancellationToken cancellationToken = default) =>
        await SaveCompletionAsync(placements, cancellationToken);

    private async Task SaveCompletionAsync(IReadOnlyDictionary<Guid, int> placements, CancellationToken cancellationToken)
    {
        if (IsHistorical)
        {
            await correction.SaveCorrectionAsync(Draft!, placements, cancellationToken);
            Draft = null;
            await LoadAsync(GameSessionId, cancellationToken);
        }
        else
        {
            await scoringUseCase.CompleteAsync(GameSessionId, placements, DateTimeOffset.UtcNow, cancellationToken);
            CompletionSaved = true;
        }
    }

    public async Task<bool> DiscardAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return false;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            if (IsHistorical)
            {
                await correction.DeleteCompletedGameSessionAsync(GameSessionId, cancellationToken);
            }
            else
            {
                await scoringUseCase.DiscardAsync(GameSessionId, cancellationToken);
            }
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not discard Ark Nova scoring GameSession {GameSessionId}.", GameSessionId);
            ErrorMessage = UserErrorMessage.Create(AppResources.DiscardScoringFailed, exception);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshSpecialCardOptions()
    {
        if (!IsSpecialCardPickerVisible || SpecialCardTargetPlayerId is not Guid targetPlayerId)
        {
            SpecialCardOptions.Clear();
            return;
        }

        SpecialCardOptions.Clear();
        foreach (ArkNovaSpecialCardData card in _cardCatalog)
        {
            bool assignedToTarget = Players.Single(x => x.PlayerId == targetPlayerId).SpecialCards.Contains(card.Id);
            bool assignedToOtherPlayer = Players.Any(x => x.PlayerId != targetPlayerId && x.SpecialCards.Contains(card.Id));
            if (card.IsArchived && !assignedToTarget)
                continue;
            SpecialCardOptions.Add(new ArkNovaSpecialCardOptionViewModel(card, assignedToTarget, assignedToOtherPlayer));
        }
    }
}

public sealed class ArkNovaScoringPlayerViewModel
{
    private readonly ArkNovaScoringDisplayMode _displayMode;

    public ArkNovaScoringPlayerViewModel(
        ArkNovaGameSessionResultPlayerData data,
        IReadOnlyList<Guid> specialCards,
        ArkNovaScoringDisplayMode displayMode,
        bool showReputation, string zooPlanName)
    {
        PlayerId = data.PlayerId;
        Name = data.Placement is int placement ? $"{placement}. {data.Name}" : data.Name;
        Color = data.Color;
        ZooPlanNumber = data.ZooPlanNumber;
        ZooPlanText = zooPlanName;
        Attractiveness = data.Attractiveness;
        Conservation = data.Conservation;
        Reputation = data.Reputation;
        SpecialCards = specialCards;
        ShowReputation = showReputation;
        _displayMode = displayMode;
    }

    public Guid PlayerId { get; }
    public string Name { get; }
    public PlayerColor Color { get; }
    public int ZooPlanNumber { get; }
    public int? Attractiveness { get; }
    public int? Conservation { get; }
    public int? Reputation { get; }
    public IReadOnlyList<Guid> SpecialCards { get; }
    public bool ShowReputation { get; }
    public bool HasRequiredValues => Attractiveness is not null && Conservation is not null;
    public bool HasSpecialCards => SpecialCards.Count > 0;
    public string SpecialCardCountText => SpecialCards.Count.ToString();
    public Microsoft.Maui.Graphics.Color PlayerTextColor => PlayerColorPresentation.GetText(Color);
    public string ColorText => PlayerColorPresentation.GetName(Color);
    public Microsoft.Maui.Graphics.Color Tint => PlayerColorPresentation.GetBackground(Color);
    public string ZooPlanText { get; }
    public string Subtitle => $"{ColorText} · {ZooPlanText}";
    public string RawValuesText =>
        string.Format(AppResources.RawScoreFormat, Attractiveness?.ToString() ?? "–", Conservation?.ToString() ?? "–");
    public string SecondaryValuesText => ShowReputation
        ? string.Format(
            AppResources.ScoringSecondaryValuesWithReputationFormat,
            SpecialCards.Count,
            Reputation?.ToString() ?? "–")
        : string.Format(AppResources.ScoringSecondaryValuesFormat, SpecialCards.Count);
    public int? Score => HasRequiredValues
        ? _displayMode == ArkNovaScoringDisplayMode.Old
            ? ArkNovaScoringRules.CalculateOldScore(Attractiveness!.Value, Conservation!.Value)
            : ArkNovaScoringRules.CalculateNewScore(Attractiveness!.Value, Conservation!.Value)
        : null;
    public string NameWithScore => Score is int score ? string.Format(AppResources.ScoreNameFormat, Name, score) : Name;
}

public sealed class ArkNovaSpecialCardOptionViewModel(
    ArkNovaSpecialCardData card,
    bool isAssignedToTarget,
    bool isAssignedToOtherPlayer)
{
    public ArkNovaSpecialCardData Card { get; } = card;
    public string Name => Card.Name;
    public bool IsAssignedToTarget { get; } = isAssignedToTarget;
    public bool IsAssignedToOtherPlayer { get; } = isAssignedToOtherPlayer;
    public string StatusText => Card.IsArchived ? $"{AppResources.SpecialCardAssignedHere} · {AppResources.Archived}"
        : IsAssignedToTarget ? AppResources.SpecialCardAssignedHere
        : IsAssignedToOtherPlayer ? AppResources.SpecialCardAlreadyAssigned
        : AppResources.SpecialCardAvailable;
}

public enum ArkNovaScoringMode
{
    Live,
    HistoryCorrection
}

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
using BoardgameTracker.Core.Games.ArkNova.Players;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaScoringViewModel(
    ArkNovaScoringUseCase scoringUseCase,
    BoardgameTracker.Application.Games.ArkNova.GameSessions.History.ArkNovaCorrectionUseCase correction,
    IArkNovaSettingsStore settingsStore,
    ILogger<ArkNovaScoringViewModel> logger) : ObservableObject
{
    public ObservableCollection<ArkNovaScoringPlayerViewModel> Players { get; } = [];
    public ObservableCollection<ArkNovaSpecialCardOptionViewModel> SpecialCardOptions { get; } = [];

    [ObservableProperty] public partial Guid GameSessionId { get; private set; }
    [ObservableProperty] public partial string GroupName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string DateText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    [ObservableProperty] public partial bool IsSpecialCardPickerVisible { get; private set; }
    [ObservableProperty] public partial Guid? SpecialCardTargetPlayerId { get; private set; }

    public BoardgameTracker.Application.Games.ArkNova.GameSessions.History.ArkNovaCorrectionDraft? Draft { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsHistorical => Draft is not null;
    public bool IsLiveScoring => !IsHistorical;
    public string CompleteText => IsHistorical ? AppResources.SaveCorrection : AppResources.CompleteScoring;
    public string DeleteText => IsHistorical ? AppResources.DeleteGameSession : AppResources.Discard;
    public bool CanComplete => Players.Count > 0 && Players.All(x => x.HasRequiredValues);

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
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            ArkNovaScoringGameSessionData? data = await scoringUseCase.LoadAsync(gameSessionId, cancellationToken);
            if (data is null)
            {
                ErrorMessage = AppResources.ScoringNotFound;
                return;
            }

            if (data.Status == BoardgameTracker.Core.GameSessions.GameSessionStatus.Completed)
            {
                if (Draft?.Saved == true) Draft = null;
                Draft ??= await correction.BeginAsync(gameSessionId, cancellationToken);
                data = Draft.Data with { Players = Draft.Data.Players.OrderBy(p => p.Placement).ThenBy(p => p.Order).ToArray() };
            }
            OnPropertyChanged(nameof(IsHistorical));
            OnPropertyChanged(nameof(IsLiveScoring));
            OnPropertyChanged(nameof(CompleteText));
            OnPropertyChanged(nameof(DeleteText));
            ArkNovaSettings settings = settingsStore.Get();
            GameSessionId = data.GameSessionId;
            GroupName = data.GroupName;
            DateText = data.StartedAt.ToLocalTime().ToString("d", CultureInfo.CurrentCulture);
            Players.Clear();
            foreach (ArkNovaScoringPlayerData player in data.Players)
            {
                ArkNovaSpecialCard[] cards = data.SpecialCards
                    .Where(x => x.PlayerId == player.PlayerId)
                    .Select(x => x.SpecialCard)
                    .ToArray();
                Players.Add(new ArkNovaScoringPlayerViewModel(
                    player,
                    cards,
                    settings.ScoringDisplayMode,
                    settings.CaptureReputation));
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
        if (Players.All(x => x.PlayerId != playerId))
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
        ArkNovaSpecialCard card,
        CancellationToken cancellationToken = default)
    {
        CloseSpecialCardPicker();
        if (Draft is not null) Draft.SetCard(playerId, card, true);
        else await scoringUseCase.AssignSpecialCardAsync(GameSessionId, playerId, card, cancellationToken);
        await LoadAsync(GameSessionId, cancellationToken);
    }

    public async Task RemoveSpecialCardAsync(
        Guid playerId,
        ArkNovaSpecialCard card,
        CancellationToken cancellationToken = default)
    {
        CloseSpecialCardPicker();
        if (Draft is not null) Draft.SetCard(playerId, card, false);
        else await scoringUseCase.RemoveSpecialCardAsync(GameSessionId, playerId, card, cancellationToken);
        await LoadAsync(GameSessionId, cancellationToken);
    }

    public bool TryBeginNavigation()
    {
        if (IsBusy)
        {
            return false;
        }

        IsBusy = true;
        return true;
    }

    public void EndNavigation() => IsBusy = false;

    public bool IsSpecialCardAssignedToOtherPlayer(Guid playerId, ArkNovaSpecialCard card) =>
        Players.Any(x => x.PlayerId != playerId && x.SpecialCards.Contains(card));

    public async Task<ArkNovaCompletionPlan?> PrepareCompletionAsync(CancellationToken cancellationToken = default)
    {
        if (!CanComplete) return null;
        return Draft is not null ? correction.Prepare(Draft) : await scoringUseCase.PrepareCompletionAsync(GameSessionId, cancellationToken);
    }

    public async Task CompleteWithoutTieAsync(
        IReadOnlyDictionary<Guid, int> placements,
        CancellationToken cancellationToken = default) =>
        await SaveCompletionAsync(placements, cancellationToken);

    private async Task SaveCompletionAsync(IReadOnlyDictionary<Guid, int> placements, CancellationToken cancellationToken)
    {
        if (Draft is not null)
        {
            await correction.SaveAsync(Draft, placements, cancellationToken);
            Draft = null;
            await LoadAsync(GameSessionId, cancellationToken);
        }
        else await scoringUseCase.CompleteAsync(GameSessionId, placements, DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task<bool> DiscardAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return false;

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            if (IsHistorical) await correction.DeleteAsync(GameSessionId, cancellationToken);
            else await scoringUseCase.DiscardAsync(GameSessionId, cancellationToken);
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
        foreach (ArkNovaSpecialCard card in Enum.GetValues<ArkNovaSpecialCard>())
        {
            bool assignedToTarget = Players.Single(x => x.PlayerId == targetPlayerId).SpecialCards.Contains(card);
            bool assignedToOtherPlayer = Players.Any(x => x.PlayerId != targetPlayerId && x.SpecialCards.Contains(card));
            SpecialCardOptions.Add(new ArkNovaSpecialCardOptionViewModel(card, assignedToTarget, assignedToOtherPlayer));
        }
    }
}

public sealed class ArkNovaScoringPlayerViewModel
{
    private readonly ArkNovaScoringDisplayMode _displayMode;

    public ArkNovaScoringPlayerViewModel(
        ArkNovaScoringPlayerData data,
        IReadOnlyList<ArkNovaSpecialCard> specialCards,
        ArkNovaScoringDisplayMode displayMode,
        bool showReputation)
    {
        PlayerId = data.PlayerId;
        Name = data.Placement is int placement ? $"{placement}. {data.Name}" : data.Name;
        Color = data.Color;
        ZooPlanNumber = data.ZooPlanNumber;
        Attractiveness = data.Attractiveness;
        Conservation = data.Conservation;
        Reputation = data.Reputation;
        SpecialCards = specialCards;
        ShowReputation = showReputation;
        _displayMode = displayMode;
    }

    public Guid PlayerId { get; }
    public string Name { get; }
    public ArkNovaPlayerColor Color { get; }
    public int ZooPlanNumber { get; }
    public int? Attractiveness { get; }
    public int? Conservation { get; }
    public int? Reputation { get; }
    public IReadOnlyList<ArkNovaSpecialCard> SpecialCards { get; }
    public bool ShowReputation { get; }
    public bool HasRequiredValues => Attractiveness is not null && Conservation is not null;
    public bool HasSpecialCards => SpecialCards.Count > 0;
    public string SpecialCardCountText => SpecialCards.Count.ToString();
    public Microsoft.Maui.Graphics.Color PlayerTextColor => ArkNovaPlayerColorPresentation.GetText(Color);
    public string ColorText => ArkNovaPlayerColorPresentation.GetName(Color);
    public Microsoft.Maui.Graphics.Color Tint => ArkNovaPlayerColorPresentation.GetTint(Color);
    public string ZooPlanText => ArkNovaZooPlanPresentation.GetName(ZooPlanNumber);
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
    ArkNovaSpecialCard card,
    bool isAssignedToTarget,
    bool isAssignedToOtherPlayer)
{
    public ArkNovaSpecialCard Card { get; } = card;
    public string Name => ArkNovaSpecialCardPresentation.GetName(Card);
    public string Symbols => ArkNovaSpecialCardPresentation.GetSymbols(Card);
    public bool IsAssignedToTarget { get; } = isAssignedToTarget;
    public bool IsAssignedToOtherPlayer { get; } = isAssignedToOtherPlayer;
    public bool IsUnassigned => !IsAssignedToTarget && !IsAssignedToOtherPlayer;
    public string StatusText => IsAssignedToTarget
        ? AppResources.SpecialCardAssignedHere
        : IsAssignedToOtherPlayer
            ? AppResources.SpecialCardAlreadyAssigned
            : AppResources.SpecialCardAvailable;
}

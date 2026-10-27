using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.App.Diagnostics;
using BoardgameTracker.App.Features.Games.ArkNova;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.Scoring;
using BoardgameTracker.Application.Games.ArkNova.Settings;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Features.GameSessions.Scoring;

public partial class ArkNovaScoringViewModel(
    ArkNovaScoringUseCase scoringUseCase,
    IArkNovaSettingsStore settingsStore,
    ILogger<ArkNovaScoringViewModel> logger) : ObservableObject
{
    public ObservableCollection<ArkNovaScoringPlayerViewModel> Players { get; } = [];
    public ObservableCollection<ArkNovaSpecialCardOptionViewModel> SpecialCardOptions { get; } = [];

    [ObservableProperty] public partial Guid SessionId { get; private set; }
    [ObservableProperty] public partial string GroupName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string DateText { get; private set; } = string.Empty;
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    [ObservableProperty] public partial string? ErrorMessage { get; private set; }
    [ObservableProperty] public partial bool IsSpecialCardPickerVisible { get; private set; }
    [ObservableProperty] public partial Guid? SpecialCardTargetPersonId { get; private set; }

    public bool CanComplete => Players.Count > 0 && Players.All(x => x.HasRequiredValues);

    public string SpecialCardPickerTitle
    {
        get
        {
            ArkNovaScoringPlayerViewModel? player = SpecialCardTargetPersonId is Guid personId
                ? Players.FirstOrDefault(x => x.PersonId == personId)
                : null;

            return player is null
                ? AppResources.SpecialCards
                : string.Format(AppResources.SpecialCardPickerTitleFormat, player.Name);
        }
    }

    public async Task LoadAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            ArkNovaScoringSessionData? data = await scoringUseCase.LoadAsync(sessionId, cancellationToken);
            if (data is null)
            {
                ErrorMessage = "Die Wertung wurde nicht gefunden.";
                return;
            }

            ArkNovaSettings settings = settingsStore.Get();
            SessionId = data.SessionId;
            GroupName = data.GroupName;
            DateText = data.StartedAt.ToLocalTime().ToString("d", CultureInfo.CurrentCulture);
            Players.Clear();
            foreach (ArkNovaScoringPlayerData player in data.Players)
            {
                ArkNovaSpecialCard[] cards = data.SpecialCards
                    .Where(x => x.PersonId == player.PersonId)
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
            logger.LogError(exception, "Could not load Ark Nova scoring session {SessionId}.", sessionId);
            ErrorMessage = UserErrorMessage.Create("Die Wertung konnte nicht geladen werden.", exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OpenSpecialCardPicker(Guid personId)
    {
        if (Players.All(x => x.PersonId != personId))
        {
            return;
        }

        SpecialCardTargetPersonId = personId;
        IsSpecialCardPickerVisible = true;
        OnPropertyChanged(nameof(SpecialCardPickerTitle));
        RefreshSpecialCardOptions();
    }

    public void CloseSpecialCardPicker()
    {
        IsSpecialCardPickerVisible = false;
        SpecialCardTargetPersonId = null;
        SpecialCardOptions.Clear();
        OnPropertyChanged(nameof(SpecialCardPickerTitle));
    }

    public async Task AssignSpecialCardAsync(
        Guid personId,
        ArkNovaSpecialCard card,
        CancellationToken cancellationToken = default)
    {
        CloseSpecialCardPicker();
        await scoringUseCase.AssignSpecialCardAsync(SessionId, personId, card, cancellationToken);
        await LoadAsync(SessionId, cancellationToken);
    }

    public async Task RemoveSpecialCardAsync(
        Guid personId,
        ArkNovaSpecialCard card,
        CancellationToken cancellationToken = default)
    {
        CloseSpecialCardPicker();
        await scoringUseCase.RemoveSpecialCardAsync(SessionId, personId, card, cancellationToken);
        await LoadAsync(SessionId, cancellationToken);
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

    public bool IsSpecialCardAssignedToOtherPlayer(Guid personId, ArkNovaSpecialCard card) =>
        Players.Any(x => x.PersonId != personId && x.SpecialCards.Contains(card));

    public async Task<ArkNovaCompletionPlan?> PrepareCompletionAsync(CancellationToken cancellationToken = default)
    {
        if (!CanComplete) return null;
        return await scoringUseCase.PrepareCompletionAsync(SessionId, cancellationToken);
    }

    public async Task CompleteWithoutTieAsync(
        IReadOnlyDictionary<Guid, int> placements,
        CancellationToken cancellationToken = default) =>
        await scoringUseCase.CompleteAsync(SessionId, placements, DateTimeOffset.UtcNow, cancellationToken);

    public async Task<bool> DiscardAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy) return false;

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await scoringUseCase.DiscardAsync(SessionId, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not discard Ark Nova scoring session {SessionId}.", SessionId);
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
        if (!IsSpecialCardPickerVisible || SpecialCardTargetPersonId is not Guid targetPersonId)
        {
            SpecialCardOptions.Clear();
            return;
        }

        SpecialCardOptions.Clear();
        foreach (ArkNovaSpecialCard card in Enum.GetValues<ArkNovaSpecialCard>())
        {
            bool assignedToTarget = Players.Single(x => x.PersonId == targetPersonId).SpecialCards.Contains(card);
            bool assignedToOtherPlayer = Players.Any(x => x.PersonId != targetPersonId && x.SpecialCards.Contains(card));
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
        PersonId = data.PersonId;
        Name = data.Name;
        Color = data.Color;
        ZooPlanNumber = data.ZooPlanNumber;
        Attractiveness = data.Attractiveness;
        Conservation = data.Conservation;
        Reputation = data.Reputation;
        SpecialCards = specialCards;
        ShowReputation = showReputation;
        _displayMode = displayMode;
    }

    public Guid PersonId { get; }
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
    public string ColorText => ArkNovaPlayerColorPresentation.GetName(Color);
    public Microsoft.Maui.Graphics.Color Tint => ArkNovaPlayerColorPresentation.GetTint(Color);
    public string ZooPlanText => ArkNovaZooPlanPresentation.GetName(ZooPlanNumber);
    public string Subtitle => $"{ColorText} · {ZooPlanText}";
    public string RawValuesText =>
        $"Attraktivität: {Attractiveness?.ToString() ?? "–"} · Artenschutz: {Conservation?.ToString() ?? "–"}";
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
    public string NameWithScore => Score is int score ? $"{Name} · {score} Punkte" : Name;
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

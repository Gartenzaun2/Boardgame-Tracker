using System.Collections.ObjectModel;
using System.Globalization;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Scoring;

public partial class ManualScoringPlayerViewModel(Guid id, string name) : ManualParticipantViewModel(id, name)
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPoints))]
    public partial string PointsText { get; set; } = string.Empty;
    public bool HasPoints => !string.IsNullOrWhiteSpace(PointsText);
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWinner))]
    public partial int? Placement { get; set; }
    public bool IsWinner => Placement == 1;
    public bool AllowNegativePoints { get; init; }
    public bool IsWinnerSelection { get; set; }
    public bool IsRankSelection => !IsWinnerSelection;
    public ObservableCollection<ManualRankChoice> Ranks { get; } = [];
}
public sealed record ManualRankChoice(int Rank, bool IsSelected, Func<Task> ChooseAsync);
public enum ManualSubmitResult { Decision, Completed }

public partial class ManualScoringViewModel(ManualScoringQuery query, ManualScoringUseCase drafts,
    CompleteManualGameSessionUseCase complete, ManualRunningGameSessionUseCase lifecycle, ILogger<ManualScoringViewModel> logger, ManualHistoryCorrectionUseCase correction) : ObservableObject
{
    private readonly SemaphoreSlim _writes = new(1, 1);
    private ManualScoringData? _data;
    private ManualHistoricalResult? _historicalForm;
    private ScoringRulesSnapshot Rules => _historicalForm?.EditingRules ?? _data!.Session.ScoringRules;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SecondaryText))]
    public partial bool IsCorrection { get; private set; }
    public string SecondaryText => IsCorrection ? AppResources.Cancel : AppResources.Discard;
    private ManualScoreInput[] _saved = [];
    private long _version;
    private long _savedVersion;
    private int _saveRequests;
    private bool _completed;
    private bool _loaded;
    private bool _discarded;
    private Dictionary<Guid, int?> _seenPoints = [];
    public Guid SessionId => _data?.Session.SessionId ?? Guid.Empty;
    public bool HasPointsStep => _data is not null && Rules.CapturePoints;
    public ObservableCollection<ManualScoringPlayerViewModel> Players { get; } = [];
    public ObservableCollection<ManualScoringPlayerViewModel> DecisionPlayers { get; } = [];
    [ObservableProperty] public partial string GameName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string BadgeText { get; private set; } = string.Empty;
    [ObservableProperty] public partial string GroupName { get; private set; } = string.Empty;
    [ObservableProperty] public partial string Title { get; private set; } = string.Empty;
    [ObservableProperty] public partial string PrimaryText { get; private set; } = AppResources.CompleteScoring;
    [ObservableProperty] public partial bool IsPointsPage { get; private set; }
    public bool IsDecisionPage => !IsPointsPage;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial bool IsBusy { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    public partial bool IsSaving { get; private set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInteract))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    public partial bool IsConfirming { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanInteract => !IsBusy && !IsConfirming;
    public bool CanEdit => CanInteract && _loaded && !_completed && !_discarded;
    public bool CanSubmit
    {
        get
        {
            if (!CanInteract || IsSaving || _data is null || !_loaded || _discarded)
                return false;
            if (_completed)
                return true;
            if (!TryValues(out ManualScoreInput[] values))
                return false;
            ScoringRulesSnapshot rules = Rules;
            if (rules.CapturePoints && values.Any(player => player.Points is null))
                return false;
            if (IsPointsPage && ManualScoringRules.GetDecision(rules, values).Kind != ManualDecisionKind.None)
                return true;
            try
            {
                if (IsCorrection)
                    _historicalForm!.ValidateCorrection(values, _data.Session.ScoringRules);
                else
                    ManualScoringRules.ResolveFinal(rules, values);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }

    public Task LoadAsync(Guid id, bool pointsPage, CancellationToken token = default) => LoadSessionAsync(id, pointsPage, false, token);

    public Task LoadCorrectionAsync(Guid id, CancellationToken token = default) => LoadSessionAsync(id, false, true, token);

    private async Task LoadSessionAsync(Guid id, bool pointsPage, bool isCorrection, CancellationToken token)
    {
        // Returning from a confirmation or switching tabs must not reload a correction over its unsaved edits.
        if (isCorrection && IsCorrection && _loaded && SessionId == id && _historicalForm is not null)
            return;
        if (IsBusy)
            return;
        IsBusy = true;
        _loaded = false;
        ErrorMessage = null;
        try
        {
            await _writes.WaitAsync(token);
            try
            {
                _data = await query.GetAsync(id, token) ?? throw new InvalidOperationException("Manual scoring not found.");
                if (_data.Session.Status != (isCorrection ? GameSessionStatus.Completed : GameSessionStatus.Scoring))
                    throw new InvalidOperationException("The session is not in the required state.");
                _historicalForm = isCorrection ? ManualHistoricalResult.FromValues(_data.Values, _data.Session.ScoringRules) : null;
                if (isCorrection && !_historicalForm!.CanCorrect)
                    throw new InvalidOperationException("The session has no correctable result.");
                IsCorrection = isCorrection;
                _completed = false;
                _discarded = false;
                IsPointsPage = isCorrection ? Rules.CapturePoints : pointsPage;
                OnPropertyChanged(nameof(IsDecisionPage));
                GameName = _data.Session.GameName;
                BadgeText = _data.Session.BadgeText;
                GroupName = _data.Session.GroupName;
                Players.Clear();
                foreach (ManualRunningPlayer player in _data.Session.Players)
                {
                    ManualScoreInput value = _data.Values.Single(value => value.PlayerId == player.PlayerId);
                    Players.Add(new(player.PlayerId, player.Name)
                    {
                        Color = player.Color, PointsText = value.Points?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                        Placement = value.Placement, AllowNegativePoints = Rules.AllowNegativePoints
                    });
                }
                _loaded = true;
                _saved = _data.Values.ToArray();
                _seenPoints = _saved.ToDictionary(player => player.PlayerId, player => player.Points);
                RefreshDecision();
            }
            finally
            {
                _writes.Release();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load manual scoring.");
            ErrorMessage = AppResources.ScoringLoadFailed;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool TryValues(out ManualScoreInput[] values)
    {
        List<ManualScoreInput> result = [];
        foreach (ManualScoringPlayerViewModel player in Players)
        {
            int? points = null;
            if (!string.IsNullOrWhiteSpace(player.PointsText))
            {
                if (!int.TryParse(player.PointsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                    || parsed < 0 && !player.AllowNegativePoints)
                {
                    values = [];
                    return false;
                }
                points = parsed;
            }
            result.Add(new(player.PlayerId, points, player.Placement));
        }
        values = result.ToArray();
        return true;
    }
    private void RefreshDecision()
    {
        DecisionPlayers.Clear();
        if (_data is null || !TryValues(out ManualScoreInput[] values))
        {
            OnPropertyChanged(nameof(CanSubmit));
            return;
        }
        ManualDecision decision = ManualScoringRules.GetDecision(Rules, values);
        Title = IsPointsPage ? AppResources.Scoring : decision.Kind switch
        {
            ManualDecisionKind.Winners => AppResources.ManualSelectWinners,
            ManualDecisionKind.Placements => AppResources.ManualAssignPlacements,
            _ => AppResources.ManualResolveTies
        };
        PrimaryText = IsPointsPage && decision.Kind != ManualDecisionKind.None ? AppResources.ManualContinue
            : IsCorrection ? AppResources.SaveCorrection : AppResources.CompleteScoring;
        foreach (ManualScoreBlock block in decision.Blocks)
            foreach (Guid id in block.PlayerIds)
            {
                ManualScoringPlayerViewModel player = Players.Single(player => player.PlayerId == id);
                player.IsWinnerSelection = decision.Kind is ManualDecisionKind.Winners or ManualDecisionKind.WinnerTie;
                player.Ranks.Clear();
                if (!player.IsWinnerSelection)
                    foreach (int rank in Enumerable.Range(block.FirstRank, block.PlayerIds.Count))
                    {
                        int selectedRank = rank;
                        player.Ranks.Add(new(rank, rank == player.Placement, () => SelectRankAsync(player, selectedRank)));
                    }
                DecisionPlayers.Add(player);
            }
        OnPropertyChanged(nameof(CanSubmit));
    }

    public async Task PointsChangedAsync()
    {
        if (IsBusy || _completed || _data is null)
            return;
        bool parsed = TryValues(out ManualScoreInput[] current);
        bool changed = !parsed || current.Any(player => player.Points != _seenPoints.GetValueOrDefault(player.PlayerId));
        if (Rules.ResultFromPoints && changed)
            foreach (ManualScoringPlayerViewModel player in Players)
                player.Placement = null;
        if (parsed)
            _seenPoints = current.ToDictionary(player => player.PlayerId, player => player.Points);
        RefreshDecision();
        await SaveDraftAsync();
    }
    public async Task SelectWinnerAsync(ManualScoringPlayerViewModel player, bool selected)
    {
        if (!CanEdit || !DecisionPlayers.Contains(player) || player.IsWinner == selected)
            return;
        if (selected && !Rules.AllowTies)
            foreach (ManualScoringPlayerViewModel other in Players.Where(other => other != player))
                other.Placement = null;
        player.Placement = selected ? 1 : null;
        RefreshDecision();
        await SaveDraftAsync();
    }
    public async Task SelectRankAsync(ManualScoringPlayerViewModel player, int rank)
    {
        if (!CanEdit || !DecisionPlayers.Contains(player) || !player.Ranks.Any(choice => choice.Rank == rank))
            return;
        player.Placement = player.Placement == rank ? null : rank;
        RefreshDecision();
        await SaveDraftAsync();
    }
    public async Task<bool> SaveDraftAsync(CancellationToken token = default)
    {
        if (IsCorrection || _data is null || _completed || _discarded)
            return true;
        if (!TryValues(out ManualScoreInput[] values))
        {
            ErrorMessage = AppResources.ManualIntegerRequired;
            return false;
        }
        long version = Interlocked.Increment(ref _version);
        Interlocked.Increment(ref _saveRequests);
        IsSaving = true;
        try
        {
            await _writes.WaitAsync(token);
            try
            {
                if (_completed || _discarded || version <= _savedVersion)
                    return true;
                if (!values.SequenceEqual(_saved))
                    await drafts.SaveDraftAsync(SessionId, values, token);
                _saved = values;
                _savedVersion = version;
                ErrorMessage = null;
                return true;
            }
            finally
            {
                _writes.Release();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not save manual scoring draft.");
            ErrorMessage = AppResources.ManualDraftSaveFailed;
            return false;
        }
        finally
        {
            IsSaving = Interlocked.Decrement(ref _saveRequests) > 0;
        }
    }
    public async Task<bool> DiscardAsync(bool confirmed, CancellationToken token = default)
    {
        if (IsCorrection || !confirmed || !CanInteract || _data is null || _completed || _discarded)
            return false;
        IsBusy = true;
        try
        {
            await _writes.WaitAsync(token);
            try
            {
                await lifecycle.DiscardAsync(SessionId, token);
                _discarded = true;
                return true;
            }
            finally
            {
                _writes.Release();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not discard manual scoring.");
            ErrorMessage = AppResources.DiscardScoringFailed;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
    public async Task<ManualSubmitResult?> SubmitAsync(CancellationToken token = default)
    {
        if (!CanInteract || _data is null || _discarded)
            return null;
        if (_completed)
            return ManualSubmitResult.Completed;
        if (!CanSubmit || !TryValues(out ManualScoreInput[] values))
        {
            ErrorMessage = AppResources.ManualScoringInvalid;
            return null;
        }
        IsBusy = true;
        long version = Interlocked.Increment(ref _version);
        try
        {
            await _writes.WaitAsync(token);
            try
            {
                bool next = IsPointsPage && ManualScoringRules.GetDecision(Rules, values).Kind != ManualDecisionKind.None;
                if (next)
                {
                    if (IsCorrection)
                    {
                        IsPointsPage = false;
                        OnPropertyChanged(nameof(IsDecisionPage));
                        RefreshDecision();
                    }
                    else
                        await drafts.SaveDraftAsync(SessionId, values, token);
                }
                else
                {
                    if (IsCorrection)
                        await correction.SaveCorrectionAsync(SessionId, values, token);
                    else
                        await complete.CompleteAsync(SessionId, values, token);
                    _completed = true;
                }
                _saved = values;
                _savedVersion = version;
                return next ? ManualSubmitResult.Decision : ManualSubmitResult.Completed;
            }
            finally
            {
                _writes.Release();
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not submit manual scoring.");
            ErrorMessage = IsCorrection ? AppResources.CorrectionFailed : AppResources.ManualCompleteFailed;
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

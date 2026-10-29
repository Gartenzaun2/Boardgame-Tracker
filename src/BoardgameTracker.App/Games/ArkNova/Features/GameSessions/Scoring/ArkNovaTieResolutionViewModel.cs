using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using System.Collections.ObjectModel;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;

public partial class ArkNovaTieResolutionViewModel(
    ArkNovaScoringUseCase scoringUseCase,
    ArkNovaCorrectionUseCase correction) : ObservableObject
{
    public ArkNovaCorrectionDraft? Draft { get; set; }
    private readonly Dictionary<Guid, int> _scores = [];
    private readonly Dictionary<Guid, int> _placements = [];
    private readonly Dictionary<Guid, (int Min, int Max)> _allowedRanges = [];
    private readonly Dictionary<Guid, string> _names = [];

    public ObservableCollection<ArkNovaPlacementFrameViewModel> Frames { get; } = [];
    [ObservableProperty] public partial Guid GameSessionId { get; private set; }
    [ObservableProperty] public partial bool CanFinish { get; private set; }
    [ObservableProperty] public partial string? ValidationMessage { get; private set; }

    public async Task LoadAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        ArkNovaScoringGameSessionData gameSession = Draft?.Data ?? await scoringUseCase.LoadAsync(gameSessionId, cancellationToken)
            ?? throw new InvalidOperationException("The scoring GameSession does not exist.");
        ArkNovaCompletionPlan plan = Draft is not null ? correction.Prepare(Draft) : await scoringUseCase.PrepareCompletionAsync(gameSessionId, cancellationToken);

        GameSessionId = gameSessionId;
        _scores.Clear();
        _placements.Clear();
        _allowedRanges.Clear();
        _names.Clear();

        foreach (ArkNovaScoringPlayerData player in gameSession.Players)
        {
            _scores[player.PlayerId] = plan.Scores[player.PlayerId];
            _placements[player.PlayerId] = plan.DefaultPlacements[player.PlayerId];
            _names[player.PlayerId] = player.Name;
        }

        foreach (IGrouping<int, KeyValuePair<Guid, int>> scoreGroup in _scores.GroupBy(x => x.Value))
        {
            Guid[] ids = scoreGroup.Select(x => x.Key).ToArray();
            int min = ids.Min(id => plan.DefaultPlacements[id]);
            int max = min + ids.Length - 1;
            foreach (Guid id in ids)
            {
                _allowedRanges[id] = (min, max);
            }
        }

        RefreshFrames();
    }

    public bool CanMove(Guid playerId, int targetPlacement)
    {
        if (!_allowedRanges.TryGetValue(playerId, out var range) || range.Min == range.Max)
        {
            return false;
        }
        return targetPlacement >= range.Min && targetPlacement <= range.Max;
    }

    public void Move(Guid playerId, int targetPlacement)
    {
        if (!CanMove(playerId, targetPlacement))
        {
            return;
        }
        _placements[playerId] = targetPlacement;
        RefreshFrames();
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        ArkNovaPlacementRules.ValidateResolution(_scores, _placements);
        if (Draft is not null)
        {
            await correction.SaveAsync(Draft, _placements, cancellationToken);
        }
        else
        {
            await scoringUseCase.CompleteAsync(GameSessionId, _placements, DateTimeOffset.UtcNow, cancellationToken);
        }
    }

    private void RefreshFrames()
    {
        Frames.Clear();
        int playerCount = _placements.Count;
        for (int placement = 1; placement <= playerCount; placement++)
        {
            ArkNovaPlacementPlayerViewModel[] players = _placements
                .Where(x => x.Value == placement)
                .Select(x => new ArkNovaPlacementPlayerViewModel(
                    x.Key,
                    _names[x.Key],
                    _scores[x.Key],
                    _allowedRanges[x.Key].Min != _allowedRanges[x.Key].Max))
                .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            Frames.Add(new ArkNovaPlacementFrameViewModel(placement, players));
        }

        try
        {
            ArkNovaPlacementRules.ValidateResolution(_scores, _placements);
            CanFinish = true;
            ValidationMessage = null;
        }
        catch (ArgumentException)
        {
            CanFinish = false;
            ValidationMessage = AppResources.TieInvalid;
        }
    }
}

public sealed record ArkNovaPlacementFrameViewModel(int Placement, IReadOnlyList<ArkNovaPlacementPlayerViewModel> Players)
{
    public string Title => string.Format(AppResources.PlaceFormat, Placement);
}

public sealed record ArkNovaPlacementPlayerViewModel(Guid PlayerId, string Name, int Score, bool IsMovable)
{
    public string ScoreText => string.Format(AppResources.PointsFormat, Score);
}

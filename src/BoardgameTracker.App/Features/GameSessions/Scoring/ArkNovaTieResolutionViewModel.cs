using System.Collections.ObjectModel;
using BoardgameTracker.Application.Games.ArkNova.Scoring;
using BoardgameTracker.Core.Games.ArkNova;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Features.GameSessions.Scoring;

public partial class ArkNovaTieResolutionViewModel(
    ArkNovaScoringUseCase scoringUseCase) : ObservableObject
{
    private readonly Dictionary<Guid, int> _scores = [];
    private readonly Dictionary<Guid, int> _placements = [];
    private readonly Dictionary<Guid, (int Min, int Max)> _allowedRanges = [];
    private readonly Dictionary<Guid, string> _names = [];

    public ObservableCollection<ArkNovaPlacementFrameViewModel> Frames { get; } = [];
    [ObservableProperty] public partial Guid SessionId { get; private set; }
    [ObservableProperty] public partial bool CanFinish { get; private set; }
    [ObservableProperty] public partial string? ValidationMessage { get; private set; }

    public async Task LoadAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        ArkNovaScoringSessionData session = await scoringUseCase.LoadAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException("The scoring session does not exist.");
        ArkNovaCompletionPlan plan = await scoringUseCase.PrepareCompletionAsync(sessionId, cancellationToken);

        SessionId = sessionId;
        _scores.Clear();
        _placements.Clear();
        _allowedRanges.Clear();
        _names.Clear();

        foreach (ArkNovaScoringPlayerData player in session.Players)
        {
            _scores[player.PersonId] = plan.Scores[player.PersonId];
            _placements[player.PersonId] = plan.DefaultPlacements[player.PersonId];
            _names[player.PersonId] = player.Name;
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

    public bool CanMove(Guid personId, int targetPlacement)
    {
        if (!_allowedRanges.TryGetValue(personId, out var range) || range.Min == range.Max)
        {
            return false;
        }
        return targetPlacement >= range.Min && targetPlacement <= range.Max;
    }

    public void Move(Guid personId, int targetPlacement)
    {
        if (!CanMove(personId, targetPlacement)) return;
        _placements[personId] = targetPlacement;
        RefreshFrames();
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        ArkNovaPlacementRules.ValidateResolution(_scores, _placements);
        await scoringUseCase.CompleteAsync(SessionId, _placements, DateTimeOffset.UtcNow, cancellationToken);
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
            ValidationMessage = "Die aktuelle Aufteilung ist noch keine gültige Rangfolge.";
        }
    }
}

public sealed record ArkNovaPlacementFrameViewModel(int Placement, IReadOnlyList<ArkNovaPlacementPlayerViewModel> Players)
{
    public string Title => $"Platz {Placement}";
}

public sealed record ArkNovaPlacementPlayerViewModel(Guid PersonId, string Name, int Score, bool IsMovable)
{
    public string ScoreText => $"{Score} Punkte";
}

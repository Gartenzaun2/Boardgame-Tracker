using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.History;

public sealed class ArkNovaCorrectionUseCase(IArkNovaScoringDataAccess read, IArkNovaCorrectionDataAccess write)
{
    public async Task<ArkNovaCorrectionDraft> BeginAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var data = await read.LoadAsync(id, cancellationToken);
        if (data?.Status != GameSessionStatus.Completed) throw new InvalidOperationException("Only completed sessions can be corrected.");
        return new(data with { Players = data.Players.OrderBy(p => p.Placement).ThenBy(p => p.Order).ToArray() });
    }
    public ArkNovaCompletionPlan Prepare(ArkNovaCorrectionDraft draft)
    {
        if (draft.Data.Status != GameSessionStatus.Completed) throw new InvalidOperationException("Not completed.");
        var scores = draft.Data.Players.ToDictionary(p => p.PlayerId, p =>
            ArkNovaScoringRules.CalculateNewScore(p.Attractiveness ?? throw new InvalidOperationException("Required score missing."),
                p.Conservation ?? throw new InvalidOperationException("Required score missing.")));
        var defaults = ArkNovaPlacementRules.CreateDefaultPlacements(scores);
        bool unchanged = draft.Original.Players.All(p => p.Attractiveness is int a && p.Conservation is int c
            && scores[p.PlayerId] == ArkNovaScoringRules.CalculateNewScore(a, c));
        if (unchanged)
        {
            var saved = draft.Original.Players.ToDictionary(p => p.PlayerId, p => p.Placement ?? 0);
            ArkNovaPlacementRules.ValidateResolution(scores, saved);
            return new(scores, saved, false);
        }
        return new(scores, defaults, scores.Values.Distinct().Count() != scores.Count);
    }
    public async Task SaveAsync(ArkNovaCorrectionDraft draft, IReadOnlyDictionary<Guid, int> placements, CancellationToken cancellationToken = default)
    {
        var current = await BeginAsync(draft.Data.GameSessionId, cancellationToken);
        if (!current.Data.Players.Select(p => p.PlayerId).ToHashSet().SetEquals(draft.Data.Players.Select(p => p.PlayerId)))
            throw new InvalidOperationException("Participants cannot change.");
        foreach (var player in draft.Data.Players)
            ArkNovaScoringRules.ValidateScoreValues(player.Attractiveness, player.Conservation, player.Reputation);
        var plan = Prepare(draft);
        ArkNovaPlacementRules.ValidateResolution(plan.Scores, placements);
        await write.SaveAsync(draft.Data, placements, cancellationToken);
        draft.Saved = true;
    }
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await BeginAsync(id, cancellationToken);
        await write.DeleteAsync(id, cancellationToken);
    }
}

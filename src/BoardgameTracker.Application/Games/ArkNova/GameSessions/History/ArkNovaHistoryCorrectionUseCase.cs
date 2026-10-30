using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.History;

public sealed class ArkNovaHistoryCorrectionUseCase(IArkNovaHistoryCorrectionStore store)
{
    public async Task<ArkNovaHistoryCorrectionDraft> LoadCorrectionDraftAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        ArkNovaGameSessionResultData? gameSessionResult = await store.LoadGameSessionResultAsync(gameSessionId, cancellationToken);
        if (gameSessionResult?.Status != GameSessionStatus.Completed)
            throw new InvalidOperationException("Only completed sessions can be corrected.");
        return new(gameSessionResult with { Players = gameSessionResult.Players.OrderBy(p => p.Placement).ThenBy(p => p.Order).ToArray() });
    }
    public ArkNovaGameSessionCompletionPlan CreateCorrectionCompletionPlan(ArkNovaHistoryCorrectionDraft draft)
    {
        if (draft.Data.Status != GameSessionStatus.Completed)
            throw new InvalidOperationException("Not completed.");
        Dictionary<Guid, int> scores = draft.Data.Players.ToDictionary(p => p.PlayerId, p =>
            ArkNovaScoringRules.CalculateNewScore(p.Attractiveness ?? throw new InvalidOperationException("Required score missing."),
                p.Conservation ?? throw new InvalidOperationException("Required score missing.")));
        IReadOnlyDictionary<Guid, int> defaultPlacements = ArkNovaPlacementRules.CreateDefaultPlacements(scores);
        bool unchanged = draft.Original.Players.All(p => p.Attractiveness is int a && p.Conservation is int c
            && scores[p.PlayerId] == ArkNovaScoringRules.CalculateNewScore(a, c));
        // Renaming a player or changing reputation/cards must not discard a saved tie decision.
        // Only changed calculated scores require new default placements.
        if (unchanged)
        {
            Dictionary<Guid, int> savedPlacements = draft.Original.Players.ToDictionary(p => p.PlayerId, p => p.Placement ?? 0);
            ArkNovaPlacementRules.ValidateResolution(scores, savedPlacements);
            return new(scores, savedPlacements, false);
        }
        return new(scores, defaultPlacements, scores.Values.Distinct().Count() != scores.Count);
    }
    public async Task SaveCorrectionAsync(ArkNovaHistoryCorrectionDraft draft, IReadOnlyDictionary<Guid, int> placements, CancellationToken cancellationToken = default)
    {
        ArkNovaHistoryCorrectionDraft currentDraft = await LoadCorrectionDraftAsync(draft.Data.GameSessionId, cancellationToken);
        if (!currentDraft.Data.Players.Select(p => p.PlayerId).ToHashSet().SetEquals(draft.Data.Players.Select(p => p.PlayerId)))
            throw new InvalidOperationException("Participants cannot change.");
        foreach (ArkNovaGameSessionResultPlayerData player in draft.Data.Players)
            ArkNovaScoringRules.ValidateScoreValues(player.Attractiveness, player.Conservation, player.Reputation);
        foreach (ArkNovaSpecialCardAssignmentData assignment in draft.Data.SpecialCards)
        {
            bool active = currentDraft.Data.CardCatalog.Any(c => c.Id == assignment.SpecialCardId && !c.IsArchived);
            bool retained = currentDraft.Data.SpecialCards.Any(c => c.PlayerId == assignment.PlayerId && c.SpecialCardId == assignment.SpecialCardId);
            if (!active && !retained)
                throw new InvalidOperationException("Archived or unknown cards cannot be newly assigned.");
        }
        ArkNovaGameSessionCompletionPlan completionPlan = CreateCorrectionCompletionPlan(draft);
        ArkNovaPlacementRules.ValidateResolution(completionPlan.Scores, placements);
        await store.SaveCorrectionAsync(draft.Data, placements, cancellationToken);
        draft.Saved = true;
    }
    public async Task DeleteCompletedGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        await LoadCorrectionDraftAsync(gameSessionId, cancellationToken);
        await store.DeleteCompletedGameSessionAsync(gameSessionId, cancellationToken);
    }
}

using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Application.Persistence;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.History;

public sealed class ArkNovaHistoryCorrectionUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory, ArkNovaGameSessionResultQuery resultQuery)
{
    public async Task<ArkNovaHistoryCorrectionDraft> LoadCorrectionDraftAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        ArkNovaGameSessionResultData? gameSessionResult = await resultQuery.LoadGameSessionResultAsync(gameSessionId, cancellationToken);
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
        await PersistCorrectionAsync(draft.Data, placements, cancellationToken);
        draft.Saved = true;
    }
    public async Task DeleteCompletedGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        await LoadCorrectionDraftAsync(gameSessionId, cancellationToken);
        await DeleteCompletedResultAsync(gameSessionId, cancellationToken);
    }

    // Replace scores, placements and assignments together; incremental writes could leave a partial correction.
    private async Task PersistCorrectionAsync(ArkNovaGameSessionResultData resultData, IReadOnlyDictionary<Guid, int> placements, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        GameSession gameSession = await dbContext.GameSessions.Include(s => s.Players).SingleAsync(s => s.Id == resultData.GameSessionId, cancellationToken);
        if (gameSession.Status != GameSessionStatus.Completed || !await dbContext.Games.AnyAsync(g => g.Id == gameSession.GameId && g.Key == ArkNovaGame.Key, cancellationToken))
            throw new InvalidOperationException("Only completed Ark Nova sessions can be corrected.");
        if (!gameSession.Players.Select(p => p.PlayerId).ToHashSet().SetEquals(resultData.Players.Select(p => p.PlayerId)))
            throw new InvalidOperationException("Participants cannot change.");
        List<ArkNovaGameSessionPlayer> players = await dbContext.ArkNovaGameSessionPlayers.Where(p => p.GameSessionId == gameSession.Id).ToListAsync(cancellationToken);
        foreach (ArkNovaGameSessionPlayer player in players)
        {
            ArkNovaGameSessionResultPlayerData values = resultData.Players.Single(p => p.PlayerId == player.PlayerId);
            player.SetScoreValues(values.Attractiveness, values.Conservation, values.Reputation);
            gameSession.Players.Single(p => p.PlayerId == player.PlayerId).SetPlacement(placements[player.PlayerId]);
        }
        ArkNovaGameSessionSpecialCardAssignment[] existingCards = await dbContext.ArkNovaGameSessionSpecialCardAssignments.AsNoTracking()
            .Where(c => c.GameSessionId == gameSession.Id).ToArrayAsync(cancellationToken);
        foreach (ArkNovaSpecialCardAssignmentData assignment in resultData.SpecialCards)
        {
            bool active = await dbContext.ArkNovaSpecialCards.AnyAsync(c => c.Id == assignment.SpecialCardId && !c.IsArchived, cancellationToken);
            bool retained = existingCards.Any(c => c.PlayerId == assignment.PlayerId && c.SpecialCardId == assignment.SpecialCardId);
            if (!active && !retained)
                throw new InvalidOperationException("Archived cards cannot be newly assigned.");
        }
        await dbContext.ArkNovaGameSessionSpecialCardAssignments.Where(c => c.GameSessionId == gameSession.Id).ExecuteDeleteAsync(cancellationToken);
        dbContext.ArkNovaGameSessionSpecialCardAssignments.AddRange(resultData.SpecialCards.Select(c => new ArkNovaGameSessionSpecialCardAssignment(gameSession.Id, c.PlayerId, c.SpecialCardId)));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task DeleteCompletedResultAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        GameSession gameSession = await dbContext.GameSessions.SingleAsync(s => s.Id == gameSessionId, cancellationToken);
        if (gameSession.Status != GameSessionStatus.Completed || !await dbContext.Games.AnyAsync(g => g.Id == gameSession.GameId && g.Key == ArkNovaGame.Key, cancellationToken))
            throw new InvalidOperationException("Only completed sessions can be deleted here.");
        dbContext.GameSessions.Remove(gameSession);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using BoardgameTracker.Application.Persistence;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;

/// <summary>Coordinates incremental scoring and completion; domain types protect scoring rules.</summary>
public sealed class ArkNovaScoringUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory, ArkNovaGameSessionResultQuery resultQuery)
{
    public Task<ArkNovaGameSessionResultData?> LoadGameSessionResultAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        return resultQuery.LoadGameSessionResultAsync(gameSessionId, cancellationToken);
    }

    public async Task SavePlayerScoreAsync(
        Guid gameSessionId,
        Guid playerId,
        int? attractiveness,
        int? conservation,
        int? reputation,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        await EnsureScoringGameSessionAsync(gameSessionId, cancellationToken);
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        ArkNovaGameSessionPlayer? player = await dbContext.ArkNovaGameSessionPlayers
            .SingleOrDefaultAsync(player => player.GameSessionId == gameSessionId && player.PlayerId == playerId, cancellationToken);

        if (player is null)
        {
            throw new InvalidOperationException("The Ark Nova GameSession player does not exist.");
        }

        player.SetScoreValues(attractiveness, conservation, reputation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignSpecialCardAsync(
        Guid gameSessionId,
        Guid playerId,
        Guid specialCardId,
        CancellationToken cancellationToken = default)
    {
        ValidateSpecialCardArguments(gameSessionId, playerId, specialCardId);
        ArkNovaGameSessionResultData data = await GetScoringGameSessionAsync(gameSessionId, cancellationToken);
        if (!data.Players.Any(p => p.PlayerId == playerId)
            || !data.CardCatalog.Any(c => c.Id == specialCardId && !c.IsArchived))
            throw new InvalidOperationException("Only active cards may be assigned to an existing player.");
        await PersistSpecialCardAssignmentAsync(
            new ArkNovaGameSessionSpecialCardAssignment(gameSessionId, playerId, specialCardId),
            cancellationToken);
    }

    public async Task RemoveSpecialCardAsync(
        Guid gameSessionId,
        Guid playerId,
        Guid specialCardId,
        CancellationToken cancellationToken = default)
    {
        ValidateSpecialCardArguments(gameSessionId, playerId, specialCardId);
        await EnsureScoringGameSessionAsync(gameSessionId, cancellationToken);
        await DeleteSpecialCardAssignmentAsync(gameSessionId, playerId, specialCardId, cancellationToken);
    }

    public async Task<ArkNovaGameSessionCompletionPlan> PrepareCompletionAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        ArkNovaGameSessionResultData scoringData = await GetScoringGameSessionAsync(gameSessionId, cancellationToken);
        Dictionary<Guid, int> scores = CreateScores(scoringData);
        IReadOnlyDictionary<Guid, int> placements = ArkNovaPlacementRules.CreateDefaultPlacements(scores);

        return new ArkNovaGameSessionCompletionPlan(
            scores,
            placements,
            scores.GroupBy(x => x.Value).Any(group => group.Count() > 1));
    }

    public async Task CompleteAsync(
        Guid gameSessionId,
        IReadOnlyDictionary<Guid, int> placements,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(placements);
        ArkNovaGameSessionResultData scoringData = await GetScoringGameSessionAsync(gameSessionId, cancellationToken);
        Dictionary<Guid, int> scores = CreateScores(scoringData);
        ArkNovaPlacementRules.ValidateResolution(scores, placements);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        GameSession? gameSession = await dbContext.GameSessions.Include(session => session.Players)
            .SingleOrDefaultAsync(session => session.Id == gameSessionId, cancellationToken);
        if (gameSession is null || gameSession.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a GameSession in scoring can be completed.");
        }

        foreach (GameSessionPlayer player in gameSession.Players)
        {
            player.SetPlacement(placements[player.PlayerId]);
        }

        gameSession.Complete(completedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DiscardAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureScoringGameSessionAsync(gameSessionId, cancellationToken);
        await DeleteScoringGameSessionAsync(gameSessionId, cancellationToken);
    }

    private async Task EnsureScoringGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken)
    {
        ArkNovaGameSessionResultData? gameSession = await resultQuery.LoadGameSessionResultAsync(gameSessionId, cancellationToken);
        if (gameSession is null || gameSession.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a GameSession in scoring can be changed here.");
        }
    }
    private async Task<ArkNovaGameSessionResultData> GetScoringGameSessionAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArkNovaGameSessionResultData? gameSession = await resultQuery.LoadGameSessionResultAsync(gameSessionId, cancellationToken);

        if (gameSession is null)
        {
            throw new InvalidOperationException("The Ark Nova GameSession does not exist.");
        }

        if (gameSession.Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException("Only a GameSession in scoring can be changed here.");
        }

        return gameSession;
    }

    private static Dictionary<Guid, int> CreateScores(ArkNovaGameSessionResultData scoringData)
    {
        if (scoringData.Players.Count == 0 || scoringData.Players.Any(x => x.Attractiveness is null || x.Conservation is null))
        {
            throw new InvalidOperationException("Attractiveness and conservation are required for every player.");
        }

        return scoringData.Players.ToDictionary(
            player => player.PlayerId,
            player => ArkNovaScoringRules.CalculateNewScore(
                player.Attractiveness!.Value,
                player.Conservation!.Value));
    }

    private static void ValidateSpecialCardArguments(Guid gameSessionId, Guid playerId, Guid specialCardId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(specialCardId, Guid.Empty);
    }

    private async Task PersistSpecialCardAssignmentAsync(
        ArkNovaGameSessionSpecialCardAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (!await dbContext.ArkNovaSpecialCards.AnyAsync(c => c.Id == assignment.SpecialCardId && !c.IsArchived, cancellationToken))
            throw new InvalidOperationException("Only active special cards may be assigned.");
        bool exists = await dbContext.ArkNovaGameSessionSpecialCardAssignments.AnyAsync(
            x => x.GameSessionId == assignment.GameSessionId
                 && x.PlayerId == assignment.PlayerId
                 && x.SpecialCardId == assignment.SpecialCardId,
            cancellationToken);

        if (!exists)
        {
            dbContext.ArkNovaGameSessionSpecialCardAssignments.Add(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task DeleteSpecialCardAssignmentAsync(
        Guid gameSessionId,
        Guid playerId,
        Guid specialCardId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        ArkNovaGameSessionSpecialCardAssignment? assignment = await dbContext.ArkNovaGameSessionSpecialCardAssignments
            .SingleOrDefaultAsync(
                x => x.GameSessionId == gameSessionId && x.PlayerId == playerId && x.SpecialCardId == specialCardId,
                cancellationToken);

        if (assignment is not null)
        {
            dbContext.ArkNovaGameSessionSpecialCardAssignments.Remove(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task DeleteScoringGameSessionAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        GameSession? gameSession = await dbContext.GameSessions
            .SingleOrDefaultAsync(x => x.Id == gameSessionId, cancellationToken);

        if (gameSession is not null)
        {
            dbContext.GameSessions.Remove(gameSession);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

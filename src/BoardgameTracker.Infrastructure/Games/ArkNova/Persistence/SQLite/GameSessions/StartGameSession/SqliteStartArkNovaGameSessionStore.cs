using BoardgameTracker.Application.Games.ArkNova.GameSessions.StartGameSession;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Infrastructure.Persistence.SQLite;

namespace BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.GameSessions.StartGameSession;

/// <summary>
/// SQLite implementation of the Ark Nova start-gameSession persistence boundary, saving the generic and game-specific gameSession snapshot in one SaveChanges operation.
/// </summary>
public sealed class SqliteStartArkNovaGameSessionStore(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IStartArkNovaGameSessionStore
{
    public async Task<StartArkNovaGameSessionContext?> LoadContextAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var group = await dbContext.Groups
            .AsNoTracking()
            .Where(candidate => candidate.Id == groupId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.IsArchived
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (group is null)
        {
            return null;
        }

        Guid? gameId = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.Key == ArkNovaGame.Key)
            .Select(game => (Guid?)game.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (gameId is null)
        {
            throw new InvalidOperationException(
                "Ark Nova reference data is missing from the database.");
        }

        Guid[] groupPlayerIds = await dbContext.GroupPlayers
            .AsNoTracking()
            .Where(player => player.GroupId == groupId)
            .OrderBy(player => player.DefaultOrder)
            .Select(player => player.PlayerId)
            .ToArrayAsync(cancellationToken);

        int[] zooPlanNumbers = await dbContext.ArkNovaZooPlans
            .AsNoTracking()
            .Select(plan => plan.Number)
            .ToArrayAsync(cancellationToken);

        bool hasOpenGameSession = await dbContext.GameSessions
            .AsNoTracking()
            .AnyAsync(
                gameSession => gameSession.GameId == gameId.Value
                            && gameSession.GroupId == groupId
                            && (gameSession.Status == GameSessionStatus.Running
                             || gameSession.Status == GameSessionStatus.Scoring),
                cancellationToken);

        return new StartArkNovaGameSessionContext(
            gameId.Value,
            group.IsArchived,
            hasOpenGameSession,
            groupPlayerIds,
            zooPlanNumbers.ToHashSet());
    }

    public async Task SaveAsync(
        GameSession gameSession,
        IReadOnlyCollection<ArkNovaGameSessionPlayer> arkNovaPlayers,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await dbContext.GameSessions.AddAsync(gameSession, cancellationToken);
        await dbContext.ArkNovaGameSessionPlayers.AddRangeAsync(arkNovaPlayers, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

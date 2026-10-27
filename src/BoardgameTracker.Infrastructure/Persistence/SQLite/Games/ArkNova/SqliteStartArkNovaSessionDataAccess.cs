using BoardgameTracker.Application.Games.ArkNova.StartSession;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Games.ArkNova;

/// <summary>
/// SQLite implementation of the Ark Nova start-session persistence boundary, saving the generic and game-specific session snapshot in one SaveChanges operation.
/// </summary>
public sealed class SqliteStartArkNovaSessionDataAccess(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IStartArkNovaSessionDataAccess
{
    public async Task<StartArkNovaSessionContext?> LoadContextAsync(
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

        Guid[] groupMemberIds = await dbContext.GroupPersons
            .AsNoTracking()
            .Where(member => member.GroupId == groupId)
            .OrderBy(member => member.DefaultOrder)
            .Select(member => member.PersonId)
            .ToArrayAsync(cancellationToken);

        int[] zooPlanIds = await dbContext.ArkNovaZooPlans
            .AsNoTracking()
            .Select(plan => plan.Id)
            .ToArrayAsync(cancellationToken);

        bool hasOpenSession = await dbContext.GameSessions
            .AsNoTracking()
            .AnyAsync(
                session => session.Status == GameSessionStatus.Running
                           || session.Status == GameSessionStatus.Scoring,
                cancellationToken);

        return new StartArkNovaSessionContext(
            gameId.Value,
            group.IsArchived,
            hasOpenSession,
            groupMemberIds,
            zooPlanIds.ToHashSet());
    }

    public async Task SaveAsync(
        GameSession session,
        IReadOnlyCollection<ArkNovaSessionPlayer> arkNovaPlayers,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await dbContext.GameSessions.AddAsync(session, cancellationToken);
        await dbContext.ArkNovaSessionPlayers.AddRangeAsync(arkNovaPlayers, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

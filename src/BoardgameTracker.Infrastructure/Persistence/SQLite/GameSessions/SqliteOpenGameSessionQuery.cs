using BoardgameTracker.Application.GameSessions.Resume;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.GameSessions;

/// <summary>
/// SQLite implementation for retrieving unfinished gameSessions used by navigation restoration.
/// </summary>
public sealed class SqliteOpenGameSessionQuery(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IOpenGameSessionQuery
{
    public async Task<OpenGameSession?> GetAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await (
            from gameSession in dbContext.GameSessions.AsNoTracking()
            join game in dbContext.Games.AsNoTracking()
                on gameSession.GameId equals game.Id
            where gameSession.Id == gameSessionId
                  && (gameSession.Status == GameSessionStatus.Running
                      || gameSession.Status == GameSessionStatus.Scoring)
            select new OpenGameSession(
                gameSession.Id,
                game.Key,
                gameSession.Status,
                gameSession.ModifiedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

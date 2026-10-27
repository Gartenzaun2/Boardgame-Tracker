using BoardgameTracker.Application.GameSessions.Resume;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.GameSessions;

public sealed class SqliteOpenGameSessionQuery(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IOpenGameSessionQuery
{
    public async Task<OpenGameSession?> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        // SQLite cannot reliably order DateTimeOffset provider-side. Open
        // sessions are few, so materialize the minimal projection first.
        OpenGameSession[] openSessions = await (
            from session in dbContext.GameSessions.AsNoTracking()
            join game in dbContext.Games.AsNoTracking()
                on session.GameId equals game.Id
            where session.Status == GameSessionStatus.Running
                  || session.Status == GameSessionStatus.Scoring
            select new OpenGameSession(
                session.Id,
                game.Key,
                session.Status,
                session.ModifiedAt))
            .ToArrayAsync(cancellationToken);

        return openSessions
            .OrderByDescending(session => session.ModifiedAt)
            .FirstOrDefault();
    }
}

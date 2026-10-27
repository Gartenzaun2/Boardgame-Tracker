using BoardgameTracker.Application.GameSessions.Resume;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.GameSessions;

/// <summary>
/// SQLite implementation for retrieving unfinished sessions used by navigation restoration.
/// </summary>
public sealed class SqliteOpenGameSessionQuery(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IOpenGameSessionQuery
{
    public async Task<OpenGameSession?> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await (
            from session in dbContext.GameSessions.AsNoTracking()
            join game in dbContext.Games.AsNoTracking()
                on session.GameId equals game.Id
            where session.Id == sessionId
                  && (session.Status == GameSessionStatus.Running
                      || session.Status == GameSessionStatus.Scoring)
            select new OpenGameSession(
                session.Id,
                game.Key,
                session.Status,
                session.ModifiedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }



    public async Task<IReadOnlyList<OpenGameSession>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await (
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
            .ToListAsync(cancellationToken);
    }
}

using BoardgameTracker.Application.GameSessions.Running;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.GameSessions;

/// <summary>
/// SQLite implementation of the running GameSession lifecycle persistence port.
/// A fresh DbContext is used per operation so no tracked state leaks across use-case calls.
/// </summary>
public sealed class SqliteRunningGameSessionStore(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IRunningGameSessionStore
{
    public async Task<GameSession?> FindAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.GameSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                gameSession => gameSession.Id == gameSessionId,
                cancellationToken);
    }

    public async Task SaveAsync(
        GameSession gameSession,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gameSession);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.Attach(gameSession);
        dbContext.Entry(gameSession).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        GameSession gameSession,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gameSession);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.Attach(gameSession);
        dbContext.GameSessions.Remove(gameSession);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

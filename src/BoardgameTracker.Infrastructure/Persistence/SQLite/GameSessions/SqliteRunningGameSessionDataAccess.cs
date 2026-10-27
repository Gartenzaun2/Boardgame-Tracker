using BoardgameTracker.Application.GameSessions.Running;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.GameSessions;

/// <summary>
/// SQLite implementation of the running-session lifecycle persistence port.
/// A fresh DbContext is used per operation so no tracked state leaks across use-case calls.
/// </summary>
public sealed class SqliteRunningGameSessionDataAccess(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IRunningGameSessionDataAccess
{
    public async Task<GameSession?> FindAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.GameSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                session => session.Id == sessionId,
                cancellationToken);
    }

    public async Task SaveAsync(
        GameSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.Attach(session);
        dbContext.Entry(session).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        GameSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.Attach(session);
        dbContext.GameSessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

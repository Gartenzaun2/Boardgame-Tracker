using BoardgameTracker.Application.GameSessions.Running;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.GameSessions;

public sealed class SqliteRunningGameSessionDataAccess(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IRunningGameSessionDataAccess
{
    private BoardgameTrackerDbContext? _trackedContext;

    public async Task<GameSession?> FindAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await DisposeTrackedContextAsync();
        _trackedContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await _trackedContext.GameSessions
            .Include(session => session.Players)
            .SingleOrDefaultAsync(
                session => session.Id == sessionId,
                cancellationToken);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (_trackedContext is null)
        {
            throw new InvalidOperationException("No tracked game session has been loaded.");
        }

        try
        {
            await _trackedContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            await DisposeTrackedContextAsync();
        }
    }

    public async Task DeleteAsync(
        GameSession session,
        CancellationToken cancellationToken = default)
    {
        if (_trackedContext is null)
        {
            throw new InvalidOperationException("No tracked game session has been loaded.");
        }

        try
        {
            _trackedContext.GameSessions.Remove(session);
            await _trackedContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            await DisposeTrackedContextAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeTrackedContextAsync();
        GC.SuppressFinalize(this);
    }

    private async Task DisposeTrackedContextAsync()
    {
        if (_trackedContext is null)
        {
            return;
        }

        await _trackedContext.DisposeAsync();
        _trackedContext = null;
    }
}

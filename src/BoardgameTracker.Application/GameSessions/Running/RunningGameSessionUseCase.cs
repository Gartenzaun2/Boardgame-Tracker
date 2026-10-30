using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.GameSessions.Running;

/// <summary>Changes a running session through domain methods within one short-lived context.</summary>
public sealed class RunningGameSessionUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task BeginScoringAsync(Guid gameSessionId, DateTimeOffset endedAt, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        GameSession gameSession = await GetRunningGameSessionAsync(db, gameSessionId, cancellationToken);
        gameSession.BeginScoring(endedAt);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DiscardAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        GameSession gameSession = await GetRunningGameSessionAsync(db, gameSessionId, cancellationToken);
        db.GameSessions.Remove(gameSession);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<GameSession> GetRunningGameSessionAsync(BoardgameTrackerDbContext db,
        Guid gameSessionId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        GameSession gameSession = await db.GameSessions.SingleOrDefaultAsync(session => session.Id == gameSessionId, cancellationToken)
            ?? throw new InvalidOperationException("The game gameSession does not exist.");
        if (gameSession.Status != GameSessionStatus.Running)
            throw new InvalidOperationException("Only a running game gameSession can be changed here.");
        return gameSession;
    }
}

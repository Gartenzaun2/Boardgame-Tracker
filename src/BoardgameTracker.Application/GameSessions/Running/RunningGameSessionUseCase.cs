using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.GameSessions.Running;

public sealed class RunningGameSessionUseCase(
    IRunningGameSessionDataAccess dataAccess)
{
    public async Task BeginScoringAsync(
        Guid sessionId,
        DateTimeOffset endedAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            GameSession session = await GetRunningSessionAsync(sessionId, cancellationToken);
            session.BeginScoring(endedAt);
            await dataAccess.SaveAsync(cancellationToken);
        }
        finally
        {
            await dataAccess.DisposeAsync();
        }
    }

    public async Task DiscardAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            GameSession session = await GetRunningSessionAsync(sessionId, cancellationToken);
            await dataAccess.DeleteAsync(session, cancellationToken);
        }
        finally
        {
            await dataAccess.DisposeAsync();
        }
    }

    private async Task<GameSession> GetRunningSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(sessionId, Guid.Empty);

        GameSession? session = await dataAccess.FindAsync(sessionId, cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException("The game session does not exist.");
        }

        if (session.Status != GameSessionStatus.Running)
        {
            throw new InvalidOperationException("Only a running game session can be changed here.");
        }

        return session;
    }
}

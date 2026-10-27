using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.GameSessions.Running;

/// <summary>
/// Orchestrates lifecycle actions that are available while a game session is
/// running. Domain state transitions remain protected by <see cref="GameSession"/>.
/// </summary>
public sealed class RunningGameSessionUseCase(
    IRunningGameSessionDataAccess dataAccess)
{
    public async Task BeginScoringAsync(
        Guid sessionId,
        DateTimeOffset endedAt,
        CancellationToken cancellationToken = default)
    {
        GameSession session = await GetRunningSessionAsync(sessionId, cancellationToken);
        session.BeginScoring(endedAt);
        await dataAccess.SaveAsync(session, cancellationToken);
    }

    public async Task DiscardAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        GameSession session = await GetRunningSessionAsync(sessionId, cancellationToken);
        await dataAccess.DeleteAsync(session, cancellationToken);
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

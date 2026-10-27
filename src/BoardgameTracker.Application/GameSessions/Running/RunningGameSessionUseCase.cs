using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.GameSessions.Running;

/// <summary>
/// Orchestrates lifecycle actions that are available while a game gameSession is
/// running. Domain state transitions remain protected by <see cref="GameSession"/>.
/// </summary>
public sealed class RunningGameSessionUseCase(
    IRunningGameSessionDataAccess dataAccess)
{
    public async Task BeginScoringAsync(
        Guid gameSessionId,
        DateTimeOffset endedAt,
        CancellationToken cancellationToken = default)
    {
        GameSession gameSession = await GetRunningGameSessionAsync(gameSessionId, cancellationToken);
        gameSession.BeginScoring(endedAt);
        await dataAccess.SaveAsync(gameSession, cancellationToken);
    }

    public async Task DiscardAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        GameSession gameSession = await GetRunningGameSessionAsync(gameSessionId, cancellationToken);
        await dataAccess.DeleteAsync(gameSession, cancellationToken);
    }

    private async Task<GameSession> GetRunningGameSessionAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);

        GameSession? gameSession = await dataAccess.FindAsync(gameSessionId, cancellationToken);

        if (gameSession is null)
        {
            throw new InvalidOperationException("The game gameSession does not exist.");
        }

        if (gameSession.Status != GameSessionStatus.Running)
        {
            throw new InvalidOperationException("Only a running game gameSession can be changed here.");
        }

        return gameSession;
    }
}

using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.GameSessions.Running;

/// <summary>
/// Application-owned persistence boundary for lifecycle changes to an existing
/// game session. Each operation is self-contained; implementations must not rely
/// on a tracked persistence context surviving between calls.
/// </summary>
public interface IRunningGameSessionDataAccess
{
    Task<GameSession?> FindAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        GameSession session,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        GameSession session,
        CancellationToken cancellationToken = default);
}

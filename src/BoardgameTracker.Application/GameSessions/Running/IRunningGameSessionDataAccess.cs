using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.GameSessions.Running;

/// <summary>
/// Application-owned write boundary for changing the lifecycle of an already
/// running session. Infrastructure supplies the tracked EF Core implementation.
/// </summary>
public interface IRunningGameSessionDataAccess : IAsyncDisposable
{
    Task<GameSession?> FindAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        GameSession session,
        CancellationToken cancellationToken = default);
}

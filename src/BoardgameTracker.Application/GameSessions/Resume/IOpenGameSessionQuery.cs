using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.GameSessions.Resume;

/// <summary>
/// Read boundary used by startup/navigation restoration to inspect unfinished sessions
/// without exposing the concrete SQLite query to the App project.
/// </summary>
public interface IOpenGameSessionQuery
{
    Task<OpenGameSession?> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenGameSession>> GetAllAsync(
        CancellationToken cancellationToken = default);

}

/// <summary>
/// Minimal read model for an unfinished session. It intentionally contains
/// only the data needed to route the user back into that session.
/// </summary>
public sealed record OpenGameSession(
    Guid SessionId,
    string GameKey,
    GameSessionStatus Status,
    DateTimeOffset ModifiedAt);

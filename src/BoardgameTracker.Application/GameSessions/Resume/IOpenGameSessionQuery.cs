using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.GameSessions.Resume;

/// <summary>
/// Read boundary used by the app startup flow to discover an unfinished session
/// without exposing the concrete SQLite query to the App project.
/// </summary>
public interface IOpenGameSessionQuery
{
    Task<OpenGameSession?> ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Minimal startup read model for an unfinished session. It intentionally contains
/// only the data needed to route the user back into that session.
/// </summary>
public sealed record OpenGameSession(
    Guid SessionId,
    string GameKey,
    GameSessionStatus Status,
    DateTimeOffset ModifiedAt);

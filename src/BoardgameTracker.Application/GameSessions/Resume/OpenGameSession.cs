using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.GameSessions.Resume;

/// <summary>
/// Minimal read model for an unfinished gameSession. It intentionally contains
/// only the data needed to route the user back into that gameSession.
/// </summary>
public sealed record OpenGameSession(
    Guid GameSessionId,
    string GameKey,
    GameSessionStatus Status,
    DateTimeOffset ModifiedAt);

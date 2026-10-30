using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.StartGameSession;

/// <summary>
/// Minimal persisted context required by the start use case before it may create a
/// gameSession. This is a use-case read model, not a domain entity and not a database
/// model.
/// </summary>
public sealed record StartArkNovaGameSessionContext(
    Guid GameId,
    bool IsArchived,
    bool HasOpenGameSession,
    IReadOnlyList<Guid> GroupPlayerIds,
    IReadOnlySet<int> AvailableZooPlanIds);

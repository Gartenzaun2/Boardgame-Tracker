using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.StartGameSession;

/// <summary>
/// Describes the complete user request to turn an already prepared Ark Nova setup
/// into a persisted running game gameSession. The player setup objects are Core types
/// because they represent the same domain setup state that Core validates; no
/// second Application DTO is needed for identical data.
/// </summary>
public sealed record StartArkNovaGameSessionCommand(
    Guid GroupId,
    DateTimeOffset StartedAt,
    IReadOnlyList<ArkNovaGameSessionSetupPlayer> Players);

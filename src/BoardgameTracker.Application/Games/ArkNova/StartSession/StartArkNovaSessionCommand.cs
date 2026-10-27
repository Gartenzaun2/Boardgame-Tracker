using BoardgameTracker.Core.Games.ArkNova.Setup;

namespace BoardgameTracker.Application.Games.ArkNova.StartSession;

/// <summary>
/// Describes the complete user request to turn an already prepared Ark Nova setup
/// into a persisted running game session. The player setup objects are Core types
/// because they represent the same domain setup state that Core validates; no
/// second Application DTO is needed for identical data.
/// </summary>
public sealed record StartArkNovaSessionCommand(
    Guid GroupId,
    DateTimeOffset StartedAt,
    IReadOnlyList<ArkNovaSessionSetupPlayer> Players);

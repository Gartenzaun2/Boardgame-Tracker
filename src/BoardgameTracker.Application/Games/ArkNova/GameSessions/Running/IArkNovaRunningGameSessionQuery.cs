using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Running;

/// <summary>
/// Read boundary for the Ark Nova running GameSession screen. The App consumes this
/// contract while Infrastructure decides how the data is queried from storage.
/// </summary>
public interface IArkNovaRunningGameSessionQuery
{
    Task<ArkNovaRunningGameSessionData?> ExecuteAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Screen-oriented Application read model for one running Ark Nova gameSession. It is
/// deliberately separate from the Core GameSession aggregate because the screen
/// also needs projected group, player and zoo-plan display data.
/// </summary>
public sealed record ArkNovaRunningGameSessionData(
    Guid GameSessionId,
    string GroupName,
    DateTimeOffset StartedAt,
    IReadOnlyList<ArkNovaRunningGameSessionPlayer> Players);

/// <summary>
/// Projected player row for the running GameSession screen. This is not the persisted
/// ArkNovaGameSessionPlayer entity; it combines gameSession state with player and zoo-plan
/// display data needed by the UI.
/// </summary>
public sealed record ArkNovaRunningGameSessionPlayer(
    Guid PlayerId,
    string Name,
    int Order,
    PlayerColor Color,
    int ZooPlanNumber);

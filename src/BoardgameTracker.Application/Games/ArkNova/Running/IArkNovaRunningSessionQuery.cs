using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Games.ArkNova.Running;

/// <summary>
/// Read boundary for the Ark Nova running-session screen. The App consumes this
/// contract while Infrastructure decides how the data is queried from storage.
/// </summary>
public interface IArkNovaRunningSessionQuery
{
    Task<ArkNovaRunningSessionData?> ExecuteAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Screen-oriented Application read model for one running Ark Nova session. It is
/// deliberately separate from the Core GameSession aggregate because the screen
/// also needs projected group, person and zoo-plan display data.
/// </summary>
public sealed record ArkNovaRunningSessionData(
    Guid SessionId,
    string GroupName,
    DateTimeOffset StartedAt,
    IReadOnlyList<ArkNovaRunningSessionPlayer> Players);

/// <summary>
/// Projected player row for the running-session screen. This is not the persisted
/// ArkNovaSessionPlayer entity; it combines session state with person and zoo-plan
/// display data needed by the UI.
/// </summary>
public sealed record ArkNovaRunningSessionPlayer(
    Guid PersonId,
    string Name,
    int Order,
    ArkNovaPlayerColor Color,
    int ZooPlanNumber,
    string ZooPlanNameResourceKey,
    string ZooPlanIconSource);

using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Games.ArkNova.StartSession;

/// <summary>
/// Application-owned persistence boundary for the Start Ark Nova Session use case.
/// Infrastructure implements this contract so the use case can load/save its
/// required data without referencing EF Core or SQLite.
/// </summary>
public interface IStartArkNovaSessionDataAccess
{
    Task<StartArkNovaSessionContext?> LoadContextAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        GameSession session,
        IReadOnlyCollection<ArkNovaSessionPlayer> arkNovaPlayers,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Minimal persisted context required by the start use case before it may create a
/// session. This is a use-case read model, not a domain entity and not a database
/// model.
/// </summary>
public sealed record StartArkNovaSessionContext(
    Guid GameId,
    bool IsArchived,
    bool HasOpenSession,
    IReadOnlyList<Guid> GroupMemberIds,
    IReadOnlySet<int> AvailableZooPlanIds);

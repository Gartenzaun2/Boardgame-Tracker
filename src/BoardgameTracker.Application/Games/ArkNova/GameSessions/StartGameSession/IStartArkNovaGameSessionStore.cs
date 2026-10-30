using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.StartGameSession;

/// <summary>
/// Application-owned persistence boundary for the Start Ark Nova GameSession use case.
/// Infrastructure implements this contract so the use case can load/save its
/// required data without referencing EF Core or SQLite.
/// </summary>
public interface IStartArkNovaGameSessionStore
{
    Task<StartArkNovaGameSessionContext?> LoadContextAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        GameSession gameSession,
        IReadOnlyCollection<ArkNovaGameSessionPlayer> arkNovaPlayers,
        CancellationToken cancellationToken = default);
}

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

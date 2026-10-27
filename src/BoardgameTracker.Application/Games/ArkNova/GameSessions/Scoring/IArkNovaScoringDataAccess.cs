using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Games.ArkNova.Players;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;

/// <summary>
/// Application-owned persistence boundary for the Ark Nova scoring workflow.
/// Implementations use fresh technical contexts; domain changes are performed by the use case/core types.
/// </summary>
public interface IArkNovaScoringDataAccess
{
    Task<ArkNovaScoringGameSessionData?> LoadAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
    Task<ArkNovaGameSessionPlayer?> FindPlayerAsync(Guid gameSessionId, Guid playerId, CancellationToken cancellationToken = default);
    Task SavePlayerAsync(ArkNovaGameSessionPlayer player, CancellationToken cancellationToken = default);
    Task<GameSession?> FindGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
    Task SaveCompletedGameSessionAsync(GameSession gameSession, CancellationToken cancellationToken = default);
    Task AddSpecialCardAsync(ArkNovaGameSessionSpecialCardAssignment assignment, CancellationToken cancellationToken = default);
    Task RemoveSpecialCardAsync(Guid gameSessionId, Guid playerId, ArkNovaSpecialCard specialCard, CancellationToken cancellationToken = default);
    Task DeleteGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
}

public sealed record ArkNovaScoringGameSessionData(
    Guid GameSessionId,
    string GroupName,
    GameSessionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    IReadOnlyList<ArkNovaScoringPlayerData> Players,
    IReadOnlyList<ArkNovaSpecialCardAssignmentData> SpecialCards);

public sealed record ArkNovaScoringPlayerData(
    Guid PlayerId,
    string Name,
    int Order,
    ArkNovaPlayerColor Color,
    int ZooPlanNumber,
    int? Attractiveness,
    int? Conservation,
    int? Reputation,
    int? Placement);

public sealed record ArkNovaSpecialCardAssignmentData(Guid PlayerId, ArkNovaSpecialCard SpecialCard);

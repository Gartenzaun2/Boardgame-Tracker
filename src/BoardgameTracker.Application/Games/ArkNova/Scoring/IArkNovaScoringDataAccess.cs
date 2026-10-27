using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Games.ArkNova.Scoring;

/// <summary>
/// Application-owned persistence boundary for the Ark Nova scoring workflow.
/// Implementations use fresh technical contexts; domain changes are performed by the use case/core types.
/// </summary>
public interface IArkNovaScoringDataAccess
{
    Task<ArkNovaScoringSessionData?> LoadAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<ArkNovaSessionPlayer?> FindPlayerAsync(Guid sessionId, Guid personId, CancellationToken cancellationToken = default);
    Task SavePlayerAsync(ArkNovaSessionPlayer player, CancellationToken cancellationToken = default);
    Task<GameSession?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task SaveCompletedSessionAsync(GameSession session, CancellationToken cancellationToken = default);
    Task AddSpecialCardAsync(ArkNovaSessionSpecialCardAssignment assignment, CancellationToken cancellationToken = default);
    Task RemoveSpecialCardAsync(Guid sessionId, Guid personId, ArkNovaSpecialCard specialCard, CancellationToken cancellationToken = default);
    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public sealed record ArkNovaScoringSessionData(
    Guid SessionId,
    string GroupName,
    GameSessionStatus Status,
    IReadOnlyList<ArkNovaScoringPlayerData> Players,
    IReadOnlyList<ArkNovaSpecialCardAssignmentData> SpecialCards);

public sealed record ArkNovaScoringPlayerData(
    Guid PersonId,
    string Name,
    int Order,
    ArkNovaPlayerColor Color,
    int ZooPlanNumber,
    int? Attractiveness,
    int? Conservation,
    int? Reputation,
    int? Placement);

public sealed record ArkNovaSpecialCardAssignmentData(Guid PersonId, ArkNovaSpecialCard SpecialCard);

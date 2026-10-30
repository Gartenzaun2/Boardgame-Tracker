using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;

/// <summary>
/// Application-owned persistence boundary for the Ark Nova scoring workflow.
/// Implementations use fresh technical contexts; domain changes are performed by the use case/core types.
/// </summary>
public interface IArkNovaScoringStore
{
    Task<ArkNovaGameSessionResultData?> LoadGameSessionResultAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
    Task<ArkNovaGameSessionPlayer?> FindScoringPlayerAsync(Guid gameSessionId, Guid playerId, CancellationToken cancellationToken = default);
    Task SaveScoringPlayerAsync(ArkNovaGameSessionPlayer player, CancellationToken cancellationToken = default);
    Task<GameSession?> FindGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
    Task SaveCompletedGameSessionAsync(GameSession gameSession, CancellationToken cancellationToken = default);
    Task AssignSpecialCardAsync(ArkNovaGameSessionSpecialCardAssignment assignment, CancellationToken cancellationToken = default);
    Task RemoveSpecialCardAsync(Guid gameSessionId, Guid playerId, Guid specialCardId, CancellationToken cancellationToken = default);
    Task DeleteScoringGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
}

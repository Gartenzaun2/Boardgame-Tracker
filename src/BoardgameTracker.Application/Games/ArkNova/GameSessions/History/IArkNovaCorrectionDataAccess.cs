using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.History;

public interface IArkNovaCorrectionDataAccess
{
    Task SaveAsync(ArkNovaScoringGameSessionData data, IReadOnlyDictionary<Guid, int> placements, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
}

using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;
namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.History;

/// <summary>
/// Reads the shared result projection and atomically changes or deletes Completed GameSessions.
/// Its implementation may also implement the live-scoring port, but the use cases stay separate.
/// </summary>
public interface IArkNovaHistoryCorrectionStore
{
    Task<ArkNovaGameSessionResultData?> LoadGameSessionResultAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
    Task SaveCorrectionAsync(ArkNovaGameSessionResultData data, IReadOnlyDictionary<Guid, int> placements, CancellationToken cancellationToken = default);
    Task DeleteCompletedGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default);
}

using BoardgameTracker.Application.Persistence;
using System.Text.Json;
namespace BoardgameTracker.Application.History.Transfer;

public interface IGameTransferPersistence
{
    string GameKey { get; }
    int SchemaVersion { get; }
    Task<JsonElement> ReadSnapshotAsync(BoardgameTrackerDbContext db, CancellationToken cancellationToken);
    Task<JsonElement> ExportReferencesAsync(BoardgameTrackerDbContext db, Guid[] gameSessionIds, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, JsonElement>> ExportGameSessionsAsync(BoardgameTrackerDbContext db, Guid[] gameSessionIds, CancellationToken cancellationToken);
    Task<(int Existing, int New)> PreviewAsync(BoardgameTrackerDbContext db, BgtGame game, IReadOnlyList<BgtGameSession> gameSessions, CancellationToken cancellationToken);
    Task ImportAsync(BoardgameTrackerDbContext db, BgtGame game, IReadOnlyList<BgtGameSession> gameSessions,
        IReadOnlyDictionary<Guid, Guid> players, CancellationToken cancellationToken);
}

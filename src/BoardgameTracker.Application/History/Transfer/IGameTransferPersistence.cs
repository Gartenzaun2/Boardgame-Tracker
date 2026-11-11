using BoardgameTracker.Application.Persistence;
using System.Text.Json;
using BoardgameTracker.Core.Games;
using Microsoft.EntityFrameworkCore;
namespace BoardgameTracker.Application.History.Transfer;

public interface IGameTransferPersistence
{
    string GameKey { get; }
    bool CanHandle(string gameKey) => gameKey == GameKey;
    int SchemaVersion { get; }
    (PlannedGame[] Games, GameConflict[] Conflicts) PlanGames(BgtGame[] games, TransferSnapshot snapshot, GameResolution[] resolutions) => ([], []);
    Task<Game> ResolveGameAsync(BoardgameTrackerDbContext db, BgtGame game, PlannedGame? planned, CancellationToken cancellationToken) =>
        db.Games.SingleAsync(candidate => candidate.Key == game.Key, cancellationToken);
    Task<JsonElement> ReadSnapshotAsync(BoardgameTrackerDbContext db, CancellationToken cancellationToken);
    Task<JsonElement> ExportReferencesAsync(BoardgameTrackerDbContext db, Guid[] gameSessionIds, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, JsonElement>> ExportGameSessionsAsync(BoardgameTrackerDbContext db, Guid[] gameSessionIds, CancellationToken cancellationToken);
    Task<(int Existing, int New)> PreviewAsync(BoardgameTrackerDbContext db, BgtGame game, IReadOnlyList<BgtGameSession> gameSessions, CancellationToken cancellationToken);
    Task ImportAsync(BoardgameTrackerDbContext db, BgtGame game, IReadOnlyList<BgtGameSession> gameSessions,
        IReadOnlyDictionary<Guid, Guid> players, CancellationToken cancellationToken);
}

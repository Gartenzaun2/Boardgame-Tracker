using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;

/// <summary>Game-owned color storage on the caller's DbContext and transaction.</summary>
public interface IGroupColorPersistence
{
    string GameKey { get; }
    Task<IReadOnlyDictionary<Guid, PlayerColor?>> ReadAsync(BoardgameTrackerDbContext dbContext, Guid groupId, CancellationToken cancellationToken);
    Task ReplaceAsync(BoardgameTrackerDbContext dbContext, Guid groupId, IReadOnlyDictionary<Guid, PlayerColor?> colors, CancellationToken cancellationToken);
}

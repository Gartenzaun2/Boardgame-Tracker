namespace BoardgameTracker.Application.Players.ListPlayers;

public interface IListPlayersQuery
{
    Task<IReadOnlyList<PlayerListItem>> ExecuteAsync(bool includeGuests = false, bool includeArchived = false, CancellationToken cancellationToken = default);
}

public sealed record PlayerListItem(Guid PlayerId, string Name, bool IsGuest, bool IsActive,
    DateTimeOffset CreatedAt, DateTimeOffset? LastPlayedAt, Guid? LastGameSessionId, string? LastGameKey);

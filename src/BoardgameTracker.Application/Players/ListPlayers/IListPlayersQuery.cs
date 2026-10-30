namespace BoardgameTracker.Application.Players.ListPlayers;

public interface IListPlayersQuery
{
    Task<IReadOnlyList<PlayerListItem>> ListPlayersAsync(bool includeGuests = false, bool includeArchived = false, CancellationToken cancellationToken = default);
}

public sealed record PlayerListItem(Guid PlayerId, string Name, bool IsGuest, bool IsArchived,
    DateTimeOffset CreatedAt, DateTimeOffset? LastPlayedAt, Guid? LastGameSessionId, string? LastGameKey);

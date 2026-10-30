namespace BoardgameTracker.Application.Players.ListPlayers;

public sealed record PlayerListItem(Guid PlayerId, string Name, bool IsGuest, bool IsArchived,
    DateTimeOffset CreatedAt, DateTimeOffset? LastPlayedAt, Guid? LastGameSessionId, string? LastGameKey);

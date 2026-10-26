namespace BoardgameTracker.Core.Domain;

public sealed class GameSession
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid GameId { get; init; }

    public required Game Game { get; init; }

    public Guid GroupId { get; init; }

    public required Group Group { get; init; }

    public GameSessionStatus Status { get; set; } = GameSessionStatus.Running;

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? EndedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public ICollection<GameSessionPlayer> Players { get; } = [];
}

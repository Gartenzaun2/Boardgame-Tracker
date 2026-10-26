namespace BoardgameTracker.Core.Domain;

public sealed class GameSessionPlayer
{
    public Guid GameSessionId { get; init; }

    public required GameSession GameSession { get; init; }

    public Guid PersonId { get; init; }

    public required Person Person { get; init; }

    public int Order { get; set; }

    public int? Placement { get; set; }
}

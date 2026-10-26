namespace BoardgameTracker.Core.Domain;

public sealed class Game
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Key { get; init; }
}

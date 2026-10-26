namespace BoardgameTracker.Core.Domain;

public sealed class Person
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public bool IsGuest { get; set; }

    public bool IsActive { get; set; } = true;
}

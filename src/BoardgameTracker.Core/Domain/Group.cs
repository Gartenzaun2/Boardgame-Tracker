namespace BoardgameTracker.Core.Domain;

public sealed class Group
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public bool IsTemporary { get; set; }

    public ICollection<GroupPerson> Members { get; } = [];
}

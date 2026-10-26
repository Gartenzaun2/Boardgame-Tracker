namespace BoardgameTracker.Core.Domain;

public sealed class GroupPerson
{
    public Guid GroupId { get; init; }

    public required Group Group { get; init; }

    public Guid PersonId { get; init; }

    public required Person Person { get; init; }

    public int DefaultOrder { get; set; }
}

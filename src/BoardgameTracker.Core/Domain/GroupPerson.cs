namespace BoardgameTracker.Core.Domain;

public sealed class GroupPerson
{
    private GroupPerson()
    {
    }

    public GroupPerson(Guid groupId, Guid personId, int defaultOrder)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(personId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultOrder);

        GroupId = groupId;
        PersonId = personId;
        DefaultOrder = defaultOrder;
    }

    public Guid GroupId { get; private set; }

    public Group? Group { get; private set; }

    public Guid PersonId { get; private set; }

    public Person? Person { get; private set; }

    public int DefaultOrder { get; private set; }

    public void SetDefaultOrder(int defaultOrder)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultOrder);
        DefaultOrder = defaultOrder;
    }
}

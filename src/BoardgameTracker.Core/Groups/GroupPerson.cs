namespace BoardgameTracker.Core.Groups;

public sealed class GroupPerson
{
    private GroupPerson()
    {
    }

    internal GroupPerson(Guid groupId, Guid personId, int defaultOrder)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(personId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultOrder);

        GroupId = groupId;
        PersonId = personId;
        DefaultOrder = defaultOrder;
    }

    public Guid GroupId { get; private set; }

    public Guid PersonId { get; private set; }

    public int DefaultOrder { get; private set; }

    internal void SetDefaultOrder(int defaultOrder)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultOrder);
        DefaultOrder = defaultOrder;
    }
}

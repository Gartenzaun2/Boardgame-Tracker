namespace BoardgameTracker.Core.Groups;

/// <summary>
/// Aggregate root for a fixed group composition, its default member order and lifecycle flags.
/// </summary>
public sealed class Group
{
    private readonly List<GroupPerson> _members = [];

    private Group()
    {
    }

    public Group(
        string name,
        bool isTemporary,
        IReadOnlyList<Guid> memberIds)
    {
        ArgumentNullException.ThrowIfNull(memberIds);

        Id = Guid.NewGuid();
        SetName(name);
        IsTemporary = isTemporary;
        SetInitialMembers(memberIds);
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsTemporary { get; private set; }

    public bool IsArchived { get; private set; }

    public IReadOnlyCollection<GroupPerson> Members => _members;

    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void MakePermanent() => IsTemporary = false;

    public void SetArchived(bool isArchived) => IsArchived = isArchived;

    public void SetDefaultOrder(IReadOnlyList<Guid> orderedPersonIds)
    {
        ArgumentNullException.ThrowIfNull(orderedPersonIds);

        if (orderedPersonIds.Count != _members.Count
            || orderedPersonIds.Distinct().Count() != orderedPersonIds.Count
            || orderedPersonIds.Any(id => _members.All(member => member.PersonId != id)))
        {
            throw new ArgumentException(
                "The order must contain every group member exactly once.",
                nameof(orderedPersonIds));
        }

        for (int index = 0; index < orderedPersonIds.Count; index++)
        {
            GroupPerson member = _members.Single(
                candidate => candidate.PersonId == orderedPersonIds[index]);
            member.SetDefaultOrder(index + 1);
        }
    }

    private void SetInitialMembers(IReadOnlyList<Guid> memberIds)
    {
        if (memberIds.Count == 0)
        {
            throw new ArgumentException(
                "A group must contain at least one person.",
                nameof(memberIds));
        }

        if (memberIds.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException(
                "A group member must reference a valid person.",
                nameof(memberIds));
        }

        if (memberIds.Distinct().Count() != memberIds.Count)
        {
            throw new ArgumentException(
                "A person can only be a member of a group once.",
                nameof(memberIds));
        }

        for (int index = 0; index < memberIds.Count; index++)
        {
            _members.Add(new GroupPerson(Id, memberIds[index], index + 1));
        }
    }
}

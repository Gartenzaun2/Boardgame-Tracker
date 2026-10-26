namespace BoardgameTracker.Core.Domain;

public sealed class Group
{
    private readonly List<GroupPerson> _members = [];

    private Group()
    {
    }

    public Group(string name, bool isTemporary = false)
    {
        Id = Guid.NewGuid();
        SetName(name);
        IsTemporary = isTemporary;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsTemporary { get; private set; }

    public IReadOnlyCollection<GroupPerson> Members => _members;

    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void SetTemporary(bool isTemporary) => IsTemporary = isTemporary;
}

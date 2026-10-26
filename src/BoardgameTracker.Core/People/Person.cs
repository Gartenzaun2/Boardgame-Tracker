namespace BoardgameTracker.Core.People;

public sealed class Person
{
    private Person()
    {
    }

    public Person(string name, bool isGuest = false)
    {
        Id = Guid.NewGuid();
        SetName(name);
        IsGuest = isGuest;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsGuest { get; private set; }

    public bool IsActive { get; private set; }

    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void SetGuestStatus(bool isGuest) => IsGuest = isGuest;

    public void SetActiveStatus(bool isActive) => IsActive = isActive;
}

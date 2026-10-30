using BoardgameTracker.Core.Names;
namespace BoardgameTracker.Core.Players;

/// <summary>
/// Aggregate root for a reusable player or guest referenced by groups and game gameSessions.
/// </summary>
public sealed class Player
{
    private Player()
    {
    }

    public Player(string name, bool isGuest = false, Guid? id = null, DateTimeOffset? createdAt = null)
    {
        Id = id ?? Guid.CreateVersion7();
        ArgumentOutOfRangeException.ThrowIfEqual(Id, Guid.Empty);
        SetName(name);
        IsGuest = isGuest;
        IsArchived = false;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsGuest { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = EntityName.Normalize(name);
    }

    public void SetGuestStatus(bool isGuest) => IsGuest = isGuest;

    public void SetArchived(bool isArchived) => IsArchived = isArchived;
}

using BoardgameTracker.Core.Players;
namespace BoardgameTracker.Core.Groups;

/// <summary>
/// Aggregate root for a fixed group composition, its default player order and lifecycle flags.
/// </summary>
public sealed class Group
{
    private readonly List<GroupPlayer> _players = [];

    private Group()
    {
    }

    public Group(
        string name,
        bool isTemporary,
        IReadOnlyList<Guid> playerIds)
    {
        ArgumentNullException.ThrowIfNull(playerIds);

        Id = Guid.NewGuid();
        SetName(name);
        IsTemporary = isTemporary;
        SetInitialPlayers(playerIds);
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsTemporary { get; private set; }

    public bool IsArchived { get; private set; }

    public IReadOnlyCollection<GroupPlayer> Players => _players;

    public void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void MakePermanent() => IsTemporary = false;

    public void SetArchived(bool isArchived) => IsArchived = isArchived;

    public void SetDefaultOrder(IReadOnlyList<Guid> orderedPlayerIds)
    {
        ArgumentNullException.ThrowIfNull(orderedPlayerIds);

        if (orderedPlayerIds.Count != _players.Count
            || orderedPlayerIds.Distinct().Count() != orderedPlayerIds.Count
            || orderedPlayerIds.Any(id => _players.All(player => player.PlayerId != id)))
        {
            throw new ArgumentException(
                "The order must contain every group player exactly once.",
                nameof(orderedPlayerIds));
        }

        for (int index = 0; index < orderedPlayerIds.Count; index++)
        {
            GroupPlayer player = _players.Single(
                candidate => candidate.PlayerId == orderedPlayerIds[index]);
            player.SetDefaultOrder(index + 1);
        }
    }

    private void SetInitialPlayers(IReadOnlyList<Guid> playerIds)
    {
        if (playerIds.Count == 0)
        {
            throw new ArgumentException(
                "A group must contain at least one player.",
                nameof(playerIds));
        }

        if (playerIds.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException(
                "A group player must reference a valid player.",
                nameof(playerIds));
        }

        if (playerIds.Distinct().Count() != playerIds.Count)
        {
            throw new ArgumentException(
                "A player can only belong to a group once.",
                nameof(playerIds));
        }

        for (int index = 0; index < playerIds.Count; index++)
        {
            _players.Add(new GroupPlayer(Id, playerIds[index], index + 1));
        }
    }
}

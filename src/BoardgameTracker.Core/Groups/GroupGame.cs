namespace BoardgameTracker.Core.Groups;

/// <summary>Current game assignment, independent of historical game sessions.</summary>
public sealed class GroupGame
{
    private GroupGame() { }

    public GroupGame(Guid groupId, Guid gameId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(gameId, Guid.Empty);
        GroupId = groupId;
        GameId = gameId;
    }

    public Guid GroupId { get; private set; }
    public Guid GameId { get; private set; }
}

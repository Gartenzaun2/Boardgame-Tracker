using BoardgameTracker.Core.Games;

namespace BoardgameTracker.Core.Groups;

/// <summary>Current game assignment, independent of historical game sessions.</summary>
public sealed class GroupGame
{
    private GroupGame() { }

    private GroupGame(Guid groupId, Guid gameId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(gameId, Guid.Empty);
        GroupId = groupId;
        GameId = gameId;
    }

    /// <summary>Requires the group's complete membership, supplied by the caller.</summary>
    public static GroupGame Create(Group group, Game game)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(game);
        if (group.Players.Count == 0)
            throw new ArgumentException("The group must contain its complete, nonempty membership.", nameof(group));
        if (group.Players.Count < game.MinPlayers)
            throw new GroupPlayerMinimumNotMetException(game.Key, game.MinPlayers);
        if (group.Players.Count > game.MaxPlayers)
            throw new GroupPlayerLimitExceededException(game.Key, game.MaxPlayers);
        return new GroupGame(group.Id, game.Id);
    }

    public Guid GroupId { get; private set; }
    public Guid GameId { get; private set; }
}

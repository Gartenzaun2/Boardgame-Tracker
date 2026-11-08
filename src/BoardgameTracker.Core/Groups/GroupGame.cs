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
    public static GroupGame Create(Group group, Game game, GamePlayerLimit gamePlayerLimit)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(gamePlayerLimit);
        if (!string.Equals(game.Key, gamePlayerLimit.GameKey, StringComparison.Ordinal))
            throw new ArgumentException("The player limit must belong to the selected game.", nameof(gamePlayerLimit));
        if (group.Players.Count == 0)
            throw new ArgumentException("The group must contain its complete, nonempty membership.", nameof(group));
        if (group.Players.Count > gamePlayerLimit.MaximumPlayers)
            throw new GroupPlayerLimitExceededException(game.Key, gamePlayerLimit.MaximumPlayers);
        return new GroupGame(group.Id, game.Id);
    }

    public Guid GroupId { get; private set; }
    public Guid GameId { get; private set; }
}

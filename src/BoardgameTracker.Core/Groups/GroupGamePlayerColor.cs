using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Core.Groups;

/// <summary>Optional color default for a member of a group playing a particular game.</summary>
public sealed class GroupGamePlayerColor
{
    private GroupGamePlayerColor() { }

    public GroupGamePlayerColor(Guid groupId, Guid gameId, Guid playerId, PlayerColor defaultColor)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(gameId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        GroupId = groupId;
        GameId = gameId;
        PlayerId = playerId;
        SetDefaultColor(defaultColor);
    }

    public Guid GroupId { get; private set; }
    public Guid GameId { get; private set; }
    public Guid PlayerId { get; private set; }
    public PlayerColor DefaultColor { get; private set; }

    public void SetDefaultColor(PlayerColor color)
    {
        if (!Enum.IsDefined(color))
            throw new ArgumentOutOfRangeException(nameof(color));
        DefaultColor = color;
    }

    /// <summary>Requires the complete group membership; empty colors mean no color defaults.</summary>
    public static IReadOnlyList<GroupGamePlayerColor> Create(Group group, Guid gameId, IReadOnlyDictionary<Guid, PlayerColor> colors)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(colors);
        if (colors.Keys.Any(id => !group.Players.Any(player => player.PlayerId == id)))
            throw new ArgumentException("Only group members may receive color defaults.", nameof(colors));
        if (colors.Values.Distinct().Count() != colors.Count)
            throw new ArgumentException("Group/game colors must be unique.", nameof(colors));
        return colors.Select(pair => new GroupGamePlayerColor(group.Id, gameId, pair.Key, pair.Value)).ToArray();
    }
}

using BoardgameTracker.Core.Players;
namespace BoardgameTracker.Core.Groups;

/// <summary>
/// Represents one fixed membership inside a group together with its default seating order.
/// </summary>
public sealed class GroupPlayer
{
    private GroupPlayer()
    {
    }

    internal GroupPlayer(Guid groupId, Guid playerId, int defaultOrder)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultOrder);

        GroupId = groupId;
        PlayerId = playerId;
        DefaultOrder = defaultOrder;
    }

    public Guid GroupId { get; private set; }

    public Guid PlayerId { get; private set; }

    public int DefaultOrder { get; private set; }

    internal void SetDefaultOrder(int defaultOrder)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultOrder);
        DefaultOrder = defaultOrder;
    }
}

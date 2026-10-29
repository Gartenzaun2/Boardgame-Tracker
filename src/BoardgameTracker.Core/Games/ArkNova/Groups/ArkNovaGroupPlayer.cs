using BoardgameTracker.Core.Players;
namespace BoardgameTracker.Core.Games.ArkNova.Groups;

/// <summary>
/// Stores the Ark Nova-specific default color for one player within a group.
/// It extends group membership without making player color a global player property.
/// </summary>
public sealed class ArkNovaGroupPlayer
{
    private ArkNovaGroupPlayer()
    {
    }

    public ArkNovaGroupPlayer(Guid groupId, Guid playerId, PlayerColor defaultColor)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        ValidateColor(defaultColor);

        GroupId = groupId;
        PlayerId = playerId;
        DefaultColor = defaultColor;
    }

    public Guid GroupId { get; private set; }
    public Guid PlayerId { get; private set; }
    public PlayerColor DefaultColor { get; private set; }

    public void SetDefaultColor(PlayerColor color)
    {
        ValidateColor(color);
        DefaultColor = color;
    }

    private static void ValidateColor(PlayerColor color)
    {
        if (!BoardgameTracker.Core.Games.ArkNova.ArkNovaPlayerColors.Rules.Allows(color))
        {
            throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown Ark Nova player color.");
        }
    }
}

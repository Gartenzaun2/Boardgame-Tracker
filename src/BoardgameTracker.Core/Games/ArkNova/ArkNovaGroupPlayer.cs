namespace BoardgameTracker.Core.Games.ArkNova;

/// <summary>
/// Stores the Ark Nova-specific default color for one person within a group.
/// It extends group membership without making player color a global person property.
/// </summary>
public sealed class ArkNovaGroupPlayer
{
    private ArkNovaGroupPlayer()
    {
    }

    public ArkNovaGroupPlayer(Guid groupId, Guid personId, ArkNovaPlayerColor defaultColor)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(personId, Guid.Empty);
        ValidateColor(defaultColor);

        GroupId = groupId;
        PersonId = personId;
        DefaultColor = defaultColor;
    }

    public Guid GroupId { get; private set; }
    public Guid PersonId { get; private set; }
    public ArkNovaPlayerColor DefaultColor { get; private set; }

    public void SetDefaultColor(ArkNovaPlayerColor color)
    {
        ValidateColor(color);
        DefaultColor = color;
    }

    private static void ValidateColor(ArkNovaPlayerColor color)
    {
        if (!Enum.IsDefined(color))
        {
            throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown Ark Nova player color.");
        }
    }
}

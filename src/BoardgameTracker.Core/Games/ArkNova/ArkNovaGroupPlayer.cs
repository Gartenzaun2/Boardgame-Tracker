using BoardgameTracker.Core.Domain;

namespace BoardgameTracker.Core.Games.ArkNova;

public sealed class ArkNovaGroupPlayer
{
    private ArkNovaGroupPlayer()
    {
    }

    public ArkNovaGroupPlayer(Guid groupId, Guid personId, ArkNovaPlayerColor defaultColor)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(personId, Guid.Empty);

        GroupId = groupId;
        PersonId = personId;
        DefaultColor = defaultColor;
    }

    public Guid GroupId { get; private set; }

    public Guid PersonId { get; private set; }

    public GroupPerson? GroupPerson { get; private set; }

    public ArkNovaPlayerColor DefaultColor { get; private set; }

    public void SetDefaultColor(ArkNovaPlayerColor color) => DefaultColor = color;
}

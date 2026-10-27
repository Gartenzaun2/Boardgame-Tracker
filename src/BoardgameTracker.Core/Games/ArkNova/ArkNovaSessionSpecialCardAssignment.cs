namespace BoardgameTracker.Core.Games.ArkNova;

/// <summary>
/// Many-to-many assignment of a concrete special card to a player in one Ark Nova session.
/// The natural identity is session + person + card, so one card may belong to several players
/// but cannot be assigned twice to the same player.
/// </summary>
public sealed class ArkNovaSessionSpecialCardAssignment
{
    private ArkNovaSessionSpecialCardAssignment()
    {
    }

    public ArkNovaSessionSpecialCardAssignment(Guid gameSessionId, Guid personId, ArkNovaSpecialCard specialCard)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(personId, Guid.Empty);

        if (!Enum.IsDefined(specialCard))
        {
            throw new ArgumentOutOfRangeException(nameof(specialCard));
        }

        GameSessionId = gameSessionId;
        PersonId = personId;
        SpecialCard = specialCard;
    }

    public Guid GameSessionId { get; private set; }
    public Guid PersonId { get; private set; }
    public ArkNovaSpecialCard SpecialCard { get; private set; }
}

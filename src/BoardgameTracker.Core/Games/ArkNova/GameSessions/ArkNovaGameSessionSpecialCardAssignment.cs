using BoardgameTracker.Core.Players;
namespace BoardgameTracker.Core.Games.ArkNova.GameSessions;

/// <summary>
/// Many-to-many assignment of a concrete special card to a player in one Ark Nova gameSession.
/// The natural identity is GameSession + Player + card, so one card may belong to several players
/// but cannot be assigned twice to the same player.
/// </summary>
public sealed class ArkNovaGameSessionSpecialCardAssignment
{
    private ArkNovaGameSessionSpecialCardAssignment()
    {
    }

    public ArkNovaGameSessionSpecialCardAssignment(Guid gameSessionId, Guid playerId, ArkNovaSpecialCard specialCard)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);

        if (!Enum.IsDefined(specialCard))
        {
            throw new ArgumentOutOfRangeException(nameof(specialCard));
        }

        GameSessionId = gameSessionId;
        PlayerId = playerId;
        SpecialCard = specialCard;
    }

    public Guid GameSessionId { get; private set; }
    public Guid PlayerId { get; private set; }
    public ArkNovaSpecialCard SpecialCard { get; private set; }
}

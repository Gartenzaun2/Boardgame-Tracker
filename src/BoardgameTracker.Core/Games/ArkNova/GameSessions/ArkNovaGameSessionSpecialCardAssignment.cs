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

    public ArkNovaGameSessionSpecialCardAssignment(Guid gameSessionId, Guid playerId, Guid specialCardId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);

        ArgumentOutOfRangeException.ThrowIfEqual(specialCardId, Guid.Empty);

        GameSessionId = gameSessionId;
        PlayerId = playerId;
        SpecialCardId = specialCardId;
    }

    public Guid GameSessionId { get; private set; }
    public Guid PlayerId { get; private set; }
    public Guid SpecialCardId { get; private set; }
}

using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Core.Games.ArkNova;

public sealed class ArkNovaSessionPlayer
{
    private ArkNovaSessionPlayer()
    {
    }

    public ArkNovaSessionPlayer(
        Guid gameSessionId,
        Guid personId,
        ArkNovaPlayerColor color,
        int zooPlanOption1Id,
        int zooPlanOption2Id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(personId, Guid.Empty);

        if (zooPlanOption1Id == zooPlanOption2Id)
        {
            throw new ArgumentException("The two zoo plan options must be different.");
        }

        GameSessionId = gameSessionId;
        PersonId = personId;
        Color = color;
        ZooPlanOption1Id = zooPlanOption1Id;
        ZooPlanOption2Id = zooPlanOption2Id;
    }

    public Guid GameSessionId { get; private set; }

    public Guid PersonId { get; private set; }

    public GameSessionPlayer? GameSessionPlayer { get; private set; }

    public ArkNovaPlayerColor Color { get; private set; }

    public int ZooPlanOption1Id { get; private set; }

    public ArkNovaZooPlan? ZooPlanOption1 { get; private set; }

    public int ZooPlanOption2Id { get; private set; }

    public ArkNovaZooPlan? ZooPlanOption2 { get; private set; }

    public int? SelectedZooPlanId { get; private set; }

    public ArkNovaZooPlan? SelectedZooPlan { get; private set; }

    public int? Attractiveness { get; private set; }

    public int? Conservation { get; private set; }

    public int? Reputation { get; private set; }

    public void SetColor(ArkNovaPlayerColor color) => Color = color;

    public void SelectZooPlan(int zooPlanId)
    {
        if (zooPlanId != ZooPlanOption1Id && zooPlanId != ZooPlanOption2Id)
        {
            throw new ArgumentOutOfRangeException(
                nameof(zooPlanId),
                zooPlanId,
                "The selected zoo plan must be one of the two offered plans.");
        }

        SelectedZooPlanId = zooPlanId;
    }

    public void SetScoreValues(int? attractiveness, int? conservation, int? reputation)
    {
        Attractiveness = attractiveness;
        Conservation = conservation;
        Reputation = reputation;
    }
}

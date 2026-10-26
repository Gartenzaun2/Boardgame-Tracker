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
        int zooPlanOption2Id,
        int selectedZooPlanId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(personId, Guid.Empty);

        if (zooPlanOption1Id == zooPlanOption2Id)
        {
            throw new ArgumentException("The two zoo plan options must be different.");
        }

        if (selectedZooPlanId != zooPlanOption1Id
            && selectedZooPlanId != zooPlanOption2Id)
        {
            throw new ArgumentOutOfRangeException(
                nameof(selectedZooPlanId),
                selectedZooPlanId,
                "The selected zoo plan must be one of the two offered plans.");
        }

        GameSessionId = gameSessionId;
        PersonId = personId;
        Color = color;
        ZooPlanOption1Id = zooPlanOption1Id;
        ZooPlanOption2Id = zooPlanOption2Id;
        SelectedZooPlanId = selectedZooPlanId;
    }

    public Guid GameSessionId { get; private set; }
    public Guid PersonId { get; private set; }
    public ArkNovaPlayerColor Color { get; private set; }
    public int ZooPlanOption1Id { get; private set; }
    public int ZooPlanOption2Id { get; private set; }
    public int SelectedZooPlanId { get; private set; }
    public int? Attractiveness { get; private set; }
    public int? Conservation { get; private set; }
    public int? Reputation { get; private set; }

    public void SetColor(ArkNovaPlayerColor color) => Color = color;

    public void SetScoreValues(int? attractiveness, int? conservation, int? reputation)
    {
        Attractiveness = attractiveness;
        Conservation = conservation;
        Reputation = reputation;
    }
}

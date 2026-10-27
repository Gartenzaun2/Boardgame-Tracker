namespace BoardgameTracker.Core.Games.ArkNova;

/// <summary>
/// Represents the Ark Nova-specific snapshot for one player in a persisted game session,
/// including the actual color, offered zoo plans and the selected zoo plan.
/// </summary>
public sealed class ArkNovaSessionPlayer
{
    private ArkNovaSessionPlayer()
    {
    }

    public ArkNovaSessionPlayer(
        Guid gameSessionId,
        Guid personId,
        ArkNovaPlayerColor color,
        int zooPlanOption1Number,
        int zooPlanOption2Number,
        int selectedZooPlanNumber)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(personId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(zooPlanOption1Number);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(zooPlanOption2Number);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(selectedZooPlanNumber);
        ValidateColor(color);

        if (zooPlanOption1Number == zooPlanOption2Number)
        {
            throw new ArgumentException("The two zoo plan options must be different.");
        }

        if (selectedZooPlanNumber != zooPlanOption1Number
            && selectedZooPlanNumber != zooPlanOption2Number)
        {
            throw new ArgumentOutOfRangeException(
                nameof(selectedZooPlanNumber),
                selectedZooPlanNumber,
                "The selected zoo plan must be one of the two offered plans.");
        }

        GameSessionId = gameSessionId;
        PersonId = personId;
        Color = color;
        ZooPlanOption1Number = zooPlanOption1Number;
        ZooPlanOption2Number = zooPlanOption2Number;
        SelectedZooPlanNumber = selectedZooPlanNumber;
    }

    public Guid GameSessionId { get; private set; }
    public Guid PersonId { get; private set; }
    public ArkNovaPlayerColor Color { get; private set; }
    public int ZooPlanOption1Number { get; private set; }
    public int ZooPlanOption2Number { get; private set; }
    public int SelectedZooPlanNumber { get; private set; }
    public int? Attractiveness { get; private set; }
    public int? Conservation { get; private set; }
    public int? Reputation { get; private set; }

    public void SetColor(ArkNovaPlayerColor color)
    {
        ValidateColor(color);
        Color = color;
    }

    public void SetScoreValues(int? attractiveness, int? conservation, int? reputation)
    {
        ArkNovaScoringRules.ValidateScoreValues(attractiveness, conservation, reputation);

        Attractiveness = attractiveness;
        Conservation = conservation;
        Reputation = reputation;
    }

    private static void ValidateColor(ArkNovaPlayerColor color)
    {
        if (!Enum.IsDefined(color))
        {
            throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown Ark Nova player color.");
        }
    }
}

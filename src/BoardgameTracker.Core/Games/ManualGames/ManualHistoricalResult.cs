namespace BoardgameTracker.Core.Games.ManualGames;

public enum ManualHistoricalPlacements { None, Winners, Full }

/// <summary>The recorded result form, independent of current configuration and first-completion requirements.</summary>
public sealed record ManualHistoricalResult(bool HasPoints, ManualHistoricalPlacements Placements)
{
    public bool CanCorrect => HasPoints || Placements != ManualHistoricalPlacements.None;

    // Reuse the scoring controls as manual input; correcting points never silently recalculates placements.
    public ScoringRulesSnapshot EditingRules => new(HasPoints, HasPoints,
        Placements != ManualHistoricalPlacements.None, Placements == ManualHistoricalPlacements.Full,
        Placements != ManualHistoricalPlacements.None, false, null);

    public static ManualHistoricalResult FromValues(IReadOnlyList<ManualScoreInput> values, ScoringRulesSnapshot snapshot)
    {
        ValidateParticipants(values);
        bool points = values.All(player => player.Points.HasValue);
        if (!points && values.Any(player => player.Points.HasValue))
            throw new ArgumentException("Historical points must be complete or absent.", nameof(values));
        ManualHistoricalPlacements placements;
        if (values.All(player => player.Placement is null))
            placements = ManualHistoricalPlacements.None;
        else if (values.All(player => player.Placement is null or 1)
            && !(values.All(player => player.Placement == 1) && snapshot.CapturePlacements))
            placements = ManualHistoricalPlacements.Winners;
        else
        {
            ValidateFullPlacements(values);
            placements = ManualHistoricalPlacements.Full;
        }
        return new(points, placements);
    }

    public void ValidateCorrection(IReadOnlyList<ManualScoreInput> values, ScoringRulesSnapshot snapshot)
    {
        ValidateParticipants(values);
        if (!CanCorrect || values.Any(player => player.Points.HasValue != HasPoints))
            throw new ArgumentException("The historical points form must remain unchanged.", nameof(values));
        switch (Placements)
        {
            case ManualHistoricalPlacements.None:
                if (values.Any(player => player.Placement.HasValue))
                    throw new ArgumentException("Placements cannot be added.", nameof(values));
                break;
            case ManualHistoricalPlacements.Winners:
                if (values.Any(player => player.Placement is not (null or 1)) || !values.Any(player => player.Placement == 1))
                    throw new ArgumentException("A winner result must retain at least one winner and no other ranks.", nameof(values));
                break;
            case ManualHistoricalPlacements.Full:
                ValidateFullPlacements(values);
                break;
        }
        // With no additional persisted discriminator, the saved values must still identify the same form after reload.
        if (FromValues(values, snapshot) != this)
            throw new ArgumentException("A correction cannot convert the historical result form.", nameof(values));
    }

    private static void ValidateParticipants(IReadOnlyList<ManualScoreInput> values)
    {
        if (values.Count == 0 || values.Any(player => player.PlayerId == Guid.Empty)
            || values.Select(player => player.PlayerId).Distinct().Count() != values.Count)
            throw new ArgumentException("Each participant must occur exactly once.", nameof(values));
    }

    private static void ValidateFullPlacements(IReadOnlyList<ManualScoreInput> values)
    {
        // Historical results allow shared ranks and gaps. New-completion constraints must not reject them here.
        if (values.Any(player => player.Placement is not int rank || rank < 1 || rank > values.Count)
            || !values.Any(player => player.Placement == 1))
            throw new ArgumentException("Full historical placements need a valid rank for each player and at least one first place.", nameof(values));
    }
}

namespace BoardgameTracker.Core.Games.ArkNova;

/// <summary>
/// Contains the fixed Ark Nova final-scoring lookup and validates persisted raw score values.
/// Calculated scores are deliberately not persisted because they are fully derived from raw data.
/// </summary>
public static class ArkNovaScoringRules
{
    private static readonly int[] ConservationAddends =
    [
        -14, -12, -10, -8, -6, -4, -2, 0, 2, 4, 6, 9, 12, 15,
        18, 21, 24, 27, 30, 33, 36, 39, 42, 45, 48, 51, 54, 57,
        60, 63, 66, 69, 72, 75, 78, 81, 84, 87, 90, 93, 96, 99
    ];

    public const int MinimumAttractiveness = 0;
    public const int MaximumAttractiveness = 113;
    public const int MinimumConservation = 0;
    public const int MaximumConservation = 41;
    public const int MinimumReputation = 1;
    public const int MaximumReputation = 15;

    public static int GetConservationAddend(int conservation)
    {
        ValidateConservation(conservation);
        return ConservationAddends[conservation];
    }

    public static int CalculateNewScore(int attractiveness, int conservation)
    {
        ValidateAttractiveness(attractiveness);
        return attractiveness + GetConservationAddend(conservation);
    }

    public static int CalculateOldScore(int attractiveness, int conservation) =>
        CalculateNewScore(attractiveness, conservation) - 100;

    public static void ValidateScoreValues(int? attractiveness, int? conservation, int? reputation)
    {
        if (attractiveness is int attractivenessValue)
        {
            ValidateAttractiveness(attractivenessValue);
        }

        if (conservation is int conservationValue)
        {
            ValidateConservation(conservationValue);
        }

        if (reputation is < MinimumReputation or > MaximumReputation)
        {
            throw new ArgumentOutOfRangeException(nameof(reputation));
        }
    }

    private static void ValidateAttractiveness(int attractiveness)
    {
        if (attractiveness is < MinimumAttractiveness or > MaximumAttractiveness)
        {
            throw new ArgumentOutOfRangeException(nameof(attractiveness));
        }
    }

    private static void ValidateConservation(int conservation)
    {
        if (conservation is < MinimumConservation or > MaximumConservation)
        {
            throw new ArgumentOutOfRangeException(nameof(conservation));
        }
    }
}

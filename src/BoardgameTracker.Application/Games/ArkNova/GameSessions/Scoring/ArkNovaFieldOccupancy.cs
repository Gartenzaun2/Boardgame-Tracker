namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;

/// <summary>
/// Experimental field evidence rules, independent of image libraries and UI colors.
/// Confidence is a heuristic category, not a calibrated probability.
/// </summary>
public static class ArkNovaFieldOccupancy
{
    public static double CalculateScore(double fullColor, double coreColor, double fullDifference, double coreDifference)
    {
        ValidateFraction(fullColor);
        ValidateFraction(coreColor);
        ValidateFraction(fullDifference);
        ValidateFraction(coreDifference);
        return 0.46 * Math.Min(fullColor / 0.18, 1)
            + 0.34 * Math.Min(coreColor / 0.16, 1)
            + 0.10 * fullDifference + 0.10 * coreDifference;
    }

    public static ArkNovaRecognitionConfidence ClassifyConfidence(double score, double margin, double colorFraction)
    {
        ValidateFraction(score);
        ValidateFraction(margin);
        ValidateFraction(colorFraction);
        if (score >= 0.58 && margin >= 0.12 && colorFraction >= 0.10)
            return ArkNovaRecognitionConfidence.High;
        if (score >= 0.38 && margin >= 0.05 && colorFraction >= 0.06)
            return ArkNovaRecognitionConfidence.Medium;
        return ArkNovaRecognitionConfidence.Uncertain;
    }

    /// <summary>
    /// Reports ambiguity only. A caller must retain the original value and may
    /// only downgrade High to Uncertain; perspective never rewrites a score.
    /// </summary>
    public static bool HasConservationNeighborConflict(
        ArkNovaRecognitionConfidence confidence, double margin,
        double predictedFraction, double neighborFraction, double parallaxAlignment)
    {
        ValidateFraction(margin);
        ValidateFraction(predictedFraction);
        ValidateFraction(neighborFraction);
        if (!double.IsFinite(parallaxAlignment) || parallaxAlignment < -1 || parallaxAlignment > 1)
            throw new ArgumentOutOfRangeException(nameof(parallaxAlignment));
        if (confidence != ArkNovaRecognitionConfidence.High)
            return false;
        double maximum = Math.Max(predictedFraction, neighborFraction);
        double balance = maximum > 0 ? Math.Min(predictedFraction, neighborFraction) / maximum : 0;
        return (margin >= 0.65 && parallaxAlignment >= 0.70 && neighborFraction >= 0.06)
            || (neighborFraction >= 0.15 && balance >= 0.75);
    }

    private static void ValidateFraction(double value)
    {
        if (!double.IsFinite(value) || value < 0 || value > 1)
            throw new ArgumentOutOfRangeException(nameof(value), "Evidence fractions must be finite and between zero and one.");
    }
}

public enum ArkNovaRecognitionConfidence { Uncertain, Medium, High }

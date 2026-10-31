namespace BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

/// <summary>Repairs only floating-point roundoff at binary-mask fraction bounds.</summary>
internal static class ArkNovaMaskFraction
{
    public static double ClampRoundoff(double fraction)
    {
        // A byte-mask mean divided by 255 is mathematically in [0, 1]. OpenCV's
        // reciprocal multiplication can return 1.0000000000000002 for a full mask.
        // Materially invalid evidence must still fail instead of being concealed.
        const double tolerance = 1e-12;
        if (!double.IsFinite(fraction) || fraction < -tolerance || fraction > 1 + tolerance)
            throw new ArgumentOutOfRangeException(nameof(fraction), "Mask fraction exceeds numerical boundary tolerance.");
        return Math.Clamp(fraction, 0, 1);
    }
}

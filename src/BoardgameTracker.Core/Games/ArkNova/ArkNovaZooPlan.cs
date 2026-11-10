namespace BoardgameTracker.Core.Games.ArkNova;

/// <summary>
/// Represents a fixed Ark Nova zoo plan by its stable catalog identity.
/// </summary>
public sealed class ArkNovaZooPlan
{
    private ArkNovaZooPlan()
    {
    }

    public ArkNovaZooPlan(int number)
    {
        ValidateNumber(number);
        Number = number;
    }

    public int Number { get; private set; }

    public static bool IsValidNumber(int number) => number is >= 1 and <= 17;

    public static void ValidateNumber(int number)
    {
        if (!IsValidNumber(number))
            throw new ArgumentOutOfRangeException(nameof(number), number, "Unknown Ark Nova zoo plan.");
    }
}

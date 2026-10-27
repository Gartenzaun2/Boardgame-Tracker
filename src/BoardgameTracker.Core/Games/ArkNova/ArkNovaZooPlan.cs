namespace BoardgameTracker.Core.Games.ArkNova;

/// <summary>
/// Represents one of the fourteen fixed Ark Nova zoo plans. The printed plan number
/// is its stable domain identity and therefore also its persistence key.
/// </summary>
public sealed class ArkNovaZooPlan
{
    private ArkNovaZooPlan()
    {
    }

    public ArkNovaZooPlan(int number)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(number);
        Number = number;
    }

    public int Number { get; private set; }
}

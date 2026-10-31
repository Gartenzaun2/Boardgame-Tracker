namespace BoardgameTracker.App.Games.ArkNova;

/// <summary>
/// Maps the stable Ark Nova zoo-plan number to the local
/// sketch resource. These presentation details are intentionally not persisted.
/// </summary>
public static class ArkNovaZooPlanPresentation
{
    public static string GetIconSource(int number)
    {
        if (number is < 1 or > 14)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, "Unknown Ark Nova zoo plan.");
        }

        return $"zoo_plan_{number:00}.png";
    }
}

namespace BoardgameTracker.App.Games.ArkNova;

/// <summary>
/// Maps the stable Ark Nova zoo-plan number to the local
/// sketch resource. These presentation details are intentionally not persisted.
/// </summary>
public static class ArkNovaZooPlanPresentation
{
    public static string GetIconSource(int number)
    {
        BoardgameTracker.Core.Games.ArkNova.ArkNovaZooPlan.ValidateNumber(number);
        return number switch
        {
            15 => "ark_zoo_plan_turnier.png",
            16 => "ark_zoo_plan_0.png",
            17 => "ark_zoo_plan_a.png",
            _ => $"zoo_plan_{number:00}.png"
        };
    }

    public static string GetLabel(int number)
    {
        BoardgameTracker.Core.Games.ArkNova.ArkNovaZooPlan.ValidateNumber(number);
        return number switch
        {
            15 => BoardgameTracker.App.Resources.Localization.AppResources.TournamentZooPlanLabel,
            16 => "0",
            17 => "A",
            _ => number.ToString(System.Globalization.CultureInfo.CurrentCulture)
        };
    }
}

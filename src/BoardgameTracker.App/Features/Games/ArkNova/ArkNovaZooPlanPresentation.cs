using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App.Features.Games.ArkNova;

/// <summary>
/// Maps the stable Ark Nova zoo-plan number to localized UI text and the local
/// sketch resource. These presentation details are intentionally not persisted.
/// </summary>
public static class ArkNovaZooPlanPresentation
{
    public static string GetName(int number) => number switch
    {
        1 => AppResources.ArkNovaZooPlan01Name,
        2 => AppResources.ArkNovaZooPlan02Name,
        3 => AppResources.ArkNovaZooPlan03Name,
        4 => AppResources.ArkNovaZooPlan04Name,
        5 => AppResources.ArkNovaZooPlan05Name,
        6 => AppResources.ArkNovaZooPlan06Name,
        7 => AppResources.ArkNovaZooPlan07Name,
        8 => AppResources.ArkNovaZooPlan08Name,
        9 => AppResources.ArkNovaZooPlan09Name,
        10 => AppResources.ArkNovaZooPlan10Name,
        11 => AppResources.ArkNovaZooPlan11Name,
        12 => AppResources.ArkNovaZooPlan12Name,
        13 => AppResources.ArkNovaZooPlan13Name,
        14 => AppResources.ArkNovaZooPlan14Name,
        _ => throw new ArgumentOutOfRangeException(nameof(number), number, "Unknown Ark Nova zoo plan.")
    };

    public static string GetIconSource(int number)
    {
        if (number is < 1 or > 14)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, "Unknown Ark Nova zoo plan.");
        }

        return $"zoo_plan_{number:00}.png";
    }
}

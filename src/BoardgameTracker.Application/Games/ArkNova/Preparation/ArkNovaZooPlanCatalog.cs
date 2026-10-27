namespace BoardgameTracker.Application.Games.ArkNova.Preparation;

/// <summary>
/// Provides the stable preparation metadata for the fourteen Ark Nova zoo plans.
/// Display names are intentionally not duplicated here because the preparation UI
/// only needs the stable plan id and its local sketch resource.
/// </summary>
public static class ArkNovaZooPlanCatalog
{
    public static IReadOnlyList<ArkNovaZooPlanOption> All { get; } =
    [
        new(1, "zoo_plan_01.png"),
        new(2, "zoo_plan_02.png"),
        new(3, "zoo_plan_03.png"),
        new(4, "zoo_plan_04.png"),
        new(5, "zoo_plan_05.png"),
        new(6, "zoo_plan_06.png"),
        new(7, "zoo_plan_07.png"),
        new(8, "zoo_plan_08.png"),
        new(9, "zoo_plan_09.png"),
        new(10, "zoo_plan_10.png"),
        new(11, "zoo_plan_11.png"),
        new(12, "zoo_plan_12.png"),
        new(13, "zoo_plan_13.png"),
        new(14, "zoo_plan_14.png")
    ];
}

/// <summary>
/// Preparation option carrying only the stable zoo-plan id and the sketch resource
/// required before a session is persisted.
/// </summary>
public sealed record ArkNovaZooPlanOption(
    int Id,
    string IconSource);

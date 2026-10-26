namespace BoardgameTracker.Application.Games.ArkNova.Preparation;

public static class ArkNovaZooPlanCatalog
{
    public static IReadOnlyList<ArkNovaZooPlanOption> All { get; } =
    [
        new(1, "Aussichtsplattform", "zoo_plan_01.png"),
        new(2, "Freilaufflächen", "zoo_plan_02.png"),
        new(3, "Silbersee", "zoo_plan_03.png"),
        new(4, "Handelshafen", "zoo_plan_04.png"),
        new(5, "Parkrestaurant", "zoo_plan_05.png"),
        new(6, "Forschungsinstitut", "zoo_plan_06.png"),
        new(7, "Eisdielen", "zoo_plan_07.png"),
        new(8, "Hollywood Hills", "zoo_plan_08.png"),
        new(9, "Geozoo", "zoo_plan_09.png"),
        new(10, "Auffangstation", "zoo_plan_10.png"),
        new(11, "Höhlen", "zoo_plan_11.png"),
        new(12, "Künstliche Intelligenz", "zoo_plan_12.png"),
        new(13, "Reißbrett", "zoo_plan_13.png"),
        new(14, "Lagune", "zoo_plan_14.png")
    ];
}

public sealed record ArkNovaZooPlanOption(
    int Id,
    string Name,
    string IconSource);

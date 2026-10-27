namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;

/// <summary>
/// Provides the fourteen fixed Ark Nova zoo-plan numbers used by preparation.
/// Names and sketch resources are presentation concerns and are intentionally not
/// part of this Application catalog.
/// </summary>
public static class ArkNovaZooPlanCatalog
{
    public static IReadOnlyList<ArkNovaZooPlanOption> All { get; } =
    [
        .. Enumerable.Range(1, 14).Select(number => new ArkNovaZooPlanOption(number))
    ];
}

/// <summary>
/// Identifies one fixed Ark Nova zoo plan by its printed plan number.
/// </summary>
public sealed record ArkNovaZooPlanOption(int Number);

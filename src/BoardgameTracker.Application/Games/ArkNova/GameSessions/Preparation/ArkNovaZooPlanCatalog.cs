namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;

/// <summary>
/// Provides the fixed Ark Nova zoo-plan identities in manual selection order.
/// Names and sketch resources are presentation concerns and are intentionally not
/// part of this Application catalog.
/// </summary>
public static class ArkNovaZooPlanCatalog
{
    public static IReadOnlyList<ArkNovaZooPlanOption> All { get; } =
    [
        .. Enumerable.Range(1, 17).Select(number => new ArkNovaZooPlanOption(number))
    ];

    public static IReadOnlyList<ArkNovaZooPlanOption> DrawPool { get; } =
        [.. All.Where(plan => plan.Number <= 14)];
}

/// <summary>
/// Identifies one fixed Ark Nova zoo plan by its stable catalog identity.
/// </summary>
public sealed record ArkNovaZooPlanOption(int Number);

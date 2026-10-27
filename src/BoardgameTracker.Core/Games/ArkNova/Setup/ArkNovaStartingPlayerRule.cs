namespace BoardgameTracker.Core.Games.ArkNova.Setup;

/// <summary>
/// Selects the previous loser as next starting player; a shared last place is resolved alphabetically by player name.
/// </summary>
public static class ArkNovaStartingPlayerRule
{
    public static Guid? SelectFromPreviousPlacements(
        IReadOnlyCollection<ArkNovaPreviousPlacement> placements)
    {
        ArgumentNullException.ThrowIfNull(placements);

        if (placements.Count == 0)
        {
            return null;
        }

        int lastPlace = placements.Max(player => player.Placement);

        return placements
            .Where(player => player.Placement == lastPlace)
            .OrderBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(player => player.PersonId)
            .Select(player => (Guid?)player.PersonId)
            .FirstOrDefault();
    }
}

/// <summary>
/// Carries the minimal previous-session placement data required by the starting-player rule.
/// </summary>
public sealed record ArkNovaPreviousPlacement(
    Guid PersonId,
    string Name,
    int Placement);

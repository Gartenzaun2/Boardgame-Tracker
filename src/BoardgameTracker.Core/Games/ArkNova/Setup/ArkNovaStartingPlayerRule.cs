namespace BoardgameTracker.Core.Games.ArkNova.Setup;

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

public sealed record ArkNovaPreviousPlacement(
    Guid PersonId,
    string Name,
    int Placement);

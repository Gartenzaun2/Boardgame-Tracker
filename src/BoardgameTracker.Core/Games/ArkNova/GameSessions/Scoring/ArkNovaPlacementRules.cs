namespace BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;

/// <summary>
/// Creates and validates competition-ranking placements. Manual changes are restricted to
/// players that had the same calculated score and to that tie's original rank range.
/// </summary>
public static class ArkNovaPlacementRules
{
    public static IReadOnlyDictionary<Guid, int> CreateDefaultPlacements(
        IReadOnlyDictionary<Guid, int> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        Dictionary<Guid, int> result = [];
        int consumed = 0;

        foreach (IGrouping<int, KeyValuePair<Guid, int>> scoreGroup in scores
                     .OrderByDescending(x => x.Value)
                     .GroupBy(x => x.Value))
        {
            int placement = consumed + 1;
            foreach (KeyValuePair<Guid, int> player in scoreGroup)
            {
                result[player.Key] = placement;
            }

            consumed += scoreGroup.Count();
        }

        return result;
    }

    public static void ValidateResolution(
        IReadOnlyDictionary<Guid, int> scores,
        IReadOnlyDictionary<Guid, int> placements)
    {
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(placements);

        if (scores.Count == 0 || scores.Count != placements.Count || scores.Keys.Any(id => !placements.ContainsKey(id)))
        {
            throw new ArgumentException("Placements must exist for every scored player exactly once.", nameof(placements));
        }

        IReadOnlyDictionary<Guid, int> defaults = CreateDefaultPlacements(scores);

        foreach (IGrouping<int, KeyValuePair<Guid, int>> scoreGroup in scores
                     .OrderByDescending(x => x.Value)
                     .GroupBy(x => x.Value))
        {
            Guid[] ids = scoreGroup.Select(x => x.Key).ToArray();
            int basePlacement = defaults[ids[0]];
            int count = ids.Length;
            int[] resolved = ids.Select(id => placements[id]).Order().ToArray();

            if (count == 1)
            {
                if (resolved[0] != basePlacement)
                {
                    throw new ArgumentException("A non-tied player cannot be moved.", nameof(placements));
                }

                continue;
            }

            if (resolved.Any(p => p < basePlacement || p >= basePlacement + count))
            {
                throw new ArgumentException("A tied player was moved outside the original tie range.", nameof(placements));
            }

            int index = 0;
            while (index < resolved.Length)
            {
                int expectedPlacement = basePlacement + index;
                int actualPlacement = resolved[index];
                if (actualPlacement != expectedPlacement)
                {
                    throw new ArgumentException("Placements do not form a valid competition ranking.", nameof(placements));
                }

                int groupSize = 1;
                while (index + groupSize < resolved.Length && resolved[index + groupSize] == actualPlacement)
                {
                    groupSize++;
                }

                index += groupSize;
            }
        }
    }
}

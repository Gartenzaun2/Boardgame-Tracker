using System.Collections.ObjectModel;

namespace BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;

/// <summary>
/// Runtime resolution of one score tie, including shared places.
/// Only the final placements are persisted, not this block.
/// </summary>
public sealed class ArkNovaTieBlock
{
    private readonly Dictionary<Guid, int> _placements;

    private ArkNovaTieBlock(Guid[] playerIds, int firstPlacement, IReadOnlyDictionary<Guid, int> currentPlacements)
    {
        PlayerIds = Array.AsReadOnly(playerIds);
        AvailablePlacements = Array.AsReadOnly(Enumerable.Range(firstPlacement, playerIds.Length).ToArray());
        _placements = playerIds.ToDictionary(id => id, id => currentPlacements[id]);
        Placements = new ReadOnlyDictionary<Guid, int>(_placements);
    }

    public IReadOnlyList<Guid> PlayerIds { get; }
    public IReadOnlyList<int> AvailablePlacements { get; }
    public IReadOnlyDictionary<Guid, int> Placements { get; }

    public static IReadOnlyList<ArkNovaTieBlock> Create(
        IReadOnlyDictionary<Guid, int> scores,
        IReadOnlyDictionary<Guid, int> currentPlacements)
    {
        ArkNovaPlacementRules.ValidateResolution(scores, currentPlacements);
        List<ArkNovaTieBlock> blocks = [];
        int firstPlacement = 1;
        foreach (IGrouping<int, KeyValuePair<Guid, int>> group in scores.OrderByDescending(player => player.Value).GroupBy(player => player.Value))
        {
            Guid[] playerIds = group.OrderBy(player => currentPlacements[player.Key]).Select(player => player.Key).ToArray();
            if (playerIds.Length > 1)
                blocks.Add(new ArkNovaTieBlock(playerIds, firstPlacement, currentPlacements));

            firstPlacement += playerIds.Length;
        }
        return blocks.AsReadOnly();
    }

    public void SelectPlacement(Guid playerId, int placement)
    {
        if (!_placements.ContainsKey(playerId))
            throw new ArgumentException("The player does not belong to this tie block.", nameof(playerId));
        if (!AvailablePlacements.Contains(placement))
            throw new ArgumentOutOfRangeException(nameof(placement), "The place does not belong to this tie block.");
        _placements[playerId] = placement;
    }
}

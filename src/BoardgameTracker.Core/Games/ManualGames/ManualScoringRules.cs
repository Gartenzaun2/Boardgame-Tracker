namespace BoardgameTracker.Core.Games.ManualGames;

public sealed record ManualScoreInput(Guid PlayerId, int? Points, int? Placement);
public enum ManualDecisionKind { None, Winners, Placements, WinnerTie, PlacementTies }
public sealed record ManualScoreBlock(int FirstRank, IReadOnlyList<Guid> PlayerIds);
public sealed record ManualDecision(ManualDecisionKind Kind, IReadOnlyList<ManualScoreBlock> Blocks);

/// <summary>ManualGames-only validation of recorded results and point-equal rank blocks.</summary>
public static class ManualScoringRules
{
    public static ManualDecision GetDecision(ScoringRulesSnapshot rules, IReadOnlyList<ManualScoreInput> players)
    {
        if (!rules.CaptureWinner)
            return new(ManualDecisionKind.None, []);
        if (!rules.ResultFromPoints)
            return new(rules.CapturePlacements ? ManualDecisionKind.Placements : ManualDecisionKind.Winners,
                [new(1, players.Select(player => player.PlayerId).ToArray())]);
        if (players.Any(player => player.Points is null))
            return new(ManualDecisionKind.None, []);
        IReadOnlyList<ManualScoreBlock> blocks = PointBlocks(rules, players);
        if (!rules.CapturePlacements)
            return blocks[0].PlayerIds.Count > 1 ? new(ManualDecisionKind.WinnerTie, [blocks[0]]) : new(ManualDecisionKind.None, []);
        ManualScoreBlock[] ties = blocks.Where(block => block.PlayerIds.Count > 1).ToArray();
        return new(ties.Length > 0 ? ManualDecisionKind.PlacementTies : ManualDecisionKind.None, ties);
    }

    public static void ValidateDraft(ScoringRulesSnapshot rules, IReadOnlyList<ManualScoreInput> players)
    {
        if (players.Count == 0 || players.Any(player => player.PlayerId == Guid.Empty)
            || players.Select(player => player.PlayerId).Distinct().Count() != players.Count)
            throw new ArgumentException("Each participant must occur exactly once.", nameof(players));
        if (!rules.CapturePoints && players.Any(player => player.Points.HasValue)
            || !rules.AllowNegativePoints && players.Any(player => player.Points < 0))
            throw new ArgumentException("Points do not match the session's scoring rules.", nameof(players));
        if (!rules.CaptureWinner && players.Any(player => player.Placement.HasValue))
            throw new ArgumentException("This session does not capture results.", nameof(players));
        if (players.Any(player => player.Placement is int rank && (rank < 1 || rank > players.Count)))
            throw new ArgumentException("A placement is outside the participant range.", nameof(players));
        if (!rules.CaptureWinner)
            return;
        if (!rules.CapturePlacements)
        {
            if (players.Any(player => player.Placement is not (null or 1))
                || !rules.AllowTies && players.Count(player => player.Placement == 1) > 1)
                throw new ArgumentException("Only allowed winners may be recorded.", nameof(players));
        }
        if (!rules.ResultFromPoints)
            return;
        if (players.Any(player => player.Points is null))
        {
            if (players.Any(player => player.Placement.HasValue))
                throw new ArgumentException("Automatic decisions require complete points.", nameof(players));
            return;
        }
        IReadOnlyList<ManualScoreBlock> blocks = PointBlocks(rules, players);
        foreach (ManualScoreInput player in players.Where(player => player.Placement.HasValue))
        {
            ManualScoreBlock block = blocks.Single(block => block.PlayerIds.Contains(player.PlayerId));
            if (!rules.CapturePlacements && block.FirstRank != 1
                || rules.CapturePlacements && (player.Placement < block.FirstRank || player.Placement >= block.FirstRank + block.PlayerIds.Count))
                throw new ArgumentException("A decision cannot move a player outside their point-equal block.", nameof(players));
        }
    }

    public static IReadOnlyDictionary<Guid, int?> ResolveFinal(ScoringRulesSnapshot rules, IReadOnlyList<ManualScoreInput> players)
    {
        ValidateDraft(rules, players);
        if (rules.CapturePoints && players.Any(player => player.Points is null))
            throw new ArgumentException("All participants need recorded points.", nameof(players));
        Dictionary<Guid, int?> result = players.ToDictionary(player => player.PlayerId, player => player.Placement);
        if (!rules.CaptureWinner)
            return result;
        if (rules.ResultFromPoints)
        {
            IReadOnlyList<ManualScoreBlock> blocks = PointBlocks(rules, players);
            if (!rules.CapturePlacements)
            {
                if (blocks[0].PlayerIds.Count == 1)
                    result[blocks[0].PlayerIds[0]] = 1;
                ValidateWinners(rules, blocks[0].PlayerIds.Select(id => result[id]).ToArray());
            }
            else
            {
                foreach (ManualScoreBlock block in blocks)
                {
                    if (block.PlayerIds.Count == 1)
                        result[block.PlayerIds[0]] = block.FirstRank;
                    int?[] ranks = block.PlayerIds.Select(id => result[id]).ToArray();
                    // ValidateDraft already protects the original point block's range.
                    // Shared ranks and gaps are intentional for automatic tie resolution.
                    if (ranks.Any(rank => rank is null) || !rules.AllowTies && ranks.Distinct().Count() != ranks.Length)
                        throw new ArgumentException("The point block needs complete, permitted placements.");
                }
                // Completed ManualGames results must remain valid historical forms.
                if (!result.Values.Any(rank => rank == 1))
                    throw new ArgumentException("A complete placement result needs at least one first place.");
            }
        }
        else if (rules.CapturePlacements)
            ValidateRanks(result.Values.ToArray(), 1, rules.AllowTies);
        else
            ValidateWinners(rules, result.Values.ToArray());
        return result;
    }

    private static IReadOnlyList<ManualScoreBlock> PointBlocks(ScoringRulesSnapshot rules, IReadOnlyList<ManualScoreInput> players)
    {
        IEnumerable<ManualScoreInput> ordered = rules.PointsDirection == PointsDirection.Lowest
            ? players.OrderBy(player => player.Points) : players.OrderByDescending(player => player.Points);
        List<ManualScoreBlock> blocks = [];
        int consumed = 0;
        foreach (var group in ordered.GroupBy(player => player.Points))
        {
            blocks.Add(new(consumed + 1, group.Select(player => player.PlayerId).ToArray()));
            consumed += group.Count();
        }
        return blocks;
    }
    private static void ValidateWinners(ScoringRulesSnapshot rules, IReadOnlyList<int?> values)
    {
        int winners = values.Count(value => value == 1);
        if (winners == 0 || !rules.AllowTies && winners != 1)
            throw new ArgumentException("The required winner selection is incomplete.");
    }
    private static void ValidateRanks(IReadOnlyList<int?> values, int firstRank, bool allowTies)
    {
        if (values.Any(value => value is null))
            throw new ArgumentException("Every participant in this rank block needs a placement.");
        int expected = firstRank;
        foreach (var rank in values.GroupBy(value => value).OrderBy(group => group.Key))
        {
            if (rank.Key != expected || !allowTies && rank.Count() != 1)
                throw new ArgumentException("Placements must use competition ranks within their original block.");
            expected += rank.Count();
        }
    }
}

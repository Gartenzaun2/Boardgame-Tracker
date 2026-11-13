using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ManualGames;

namespace BoardgameTracker.Core.Tests.Games.ManualGames;

public sealed class ManualScoringRulesTests
{
    [Fact]
    public void AutomaticTopBlock_PreservesHistoricalFirstPlaceInvariantWhileAllowingGaps()
    {
        ScoringRulesSnapshot rules = Rules(new(true, false, true, true, true, true, PointsDirection.Highest));
        ManualScoreInput[] values = Values([100, 100, 100, 60], [1, 3, 3, null]);
        var result = ManualScoringRules.ResolveFinal(rules, values);
        Assert.Equal(new int?[] { 1, 3, 3, 4 }, values.Select(player => result[player.PlayerId]));
        ManualScoreInput[] withoutWinner = [values[0] with { Placement = 3 }, .. values.Skip(1)];
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(rules, withoutWinner));
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 3)]
    public void AutomaticPlacements_AllowSharedRanksAndGapsInsidePointBlock(int second, int third)
    {
        ScoringRulesSnapshot rules = Rules(new(true, false, true, true, true, true, PointsDirection.Highest));
        ManualScoreInput[] values = Values([100, 80, 80, 60], [null, second, third, null]);
        var result = ManualScoringRules.ResolveFinal(rules, values);
        Assert.Equal(new int?[] { 1, second, third, 4 }, values.Select(player => result[player.PlayerId]));
    }

    [Fact]
    public void AutomaticPlacements_KeepIndependentBlocksAndRejectInvalidDecisions()
    {
        ScoringRulesSnapshot rules = Rules(new(true, false, true, true, true, true, PointsDirection.Highest));
        ManualScoreInput[] values = Values([100, 80, 80, 60, 60, 20], [null, 3, 3, 5, 5, null]);
        var result = ManualScoringRules.ResolveFinal(rules, values);
        Assert.Equal(new int?[] { 1, 3, 3, 5, 5, 6 }, values.Select(player => result[player.PlayerId]));
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(rules with { AllowTies = false }, values));
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(rules, [values[0], values[1] with { Placement = 4 }, .. values.Skip(2)]));
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(rules, [values[0], values[1] with { Placement = null }, .. values.Skip(2)]));
    }

    private static ScoringRulesSnapshot Rules(ManualGameOptions options) => new ManualGameSession(Guid.NewGuid(),
        new ManualGame(new Game(ManualGame.CreateKey(), "Game", "G", 1, 12), options)).ScoringRulesSnapshot;
    private static ManualScoreInput[] Values(int?[] points, int?[] ranks) => points.Select((value, index) => new ManualScoreInput(Guid.NewGuid(), value, ranks[index])).ToArray();

    public static IEnumerable<object[]> Combinations()
    {
        yield return [new ManualGameOptions(), new int?[] { null, null }, new int?[] { null, null }, new int?[] { null, null }];
        yield return [new ManualGameOptions(true, true), new int?[] { 0, -2 }, new int?[] { null, null }, new int?[] { null, null }];
        yield return [new ManualGameOptions(CaptureWinner: true), new int?[] { null, null }, new int?[] { null, 1 }, new int?[] { null, 1 }];
        yield return [new ManualGameOptions(CaptureWinner: true, CapturePlacements: true), new int?[] { null, null }, new int?[] { 2, 1 }, new int?[] { 2, 1 }];
        yield return [new ManualGameOptions(true, false, true), new int?[] { 2, 1 }, new int?[] { null, 1 }, new int?[] { null, 1 }];
        yield return [new ManualGameOptions(true, false, true, true), new int?[] { 2, 1 }, new int?[] { 2, 1 }, new int?[] { 2, 1 }];
        foreach (PointsDirection direction in Enum.GetValues<PointsDirection>())
        {
            yield return [new ManualGameOptions(true, false, true, ResultFromPoints: true, PointsDirection: direction),
                new int?[] { 2, 1 }, new int?[] { null, null }, direction == PointsDirection.Highest ? new int?[] { 1, null } : new int?[] { null, 1 }];
            yield return [new ManualGameOptions(true, false, true, true, ResultFromPoints: true, PointsDirection: direction),
                new int?[] { 2, 1 }, new int?[] { null, null }, direction == PointsDirection.Highest ? new int?[] { 1, 2 } : new int?[] { 2, 1 }];
        }
    }
    [Theory]
    [MemberData(nameof(Combinations))]
    public void EveryResultKind_UsesOnlyItsSnapshot(ManualGameOptions options, int?[] points, int?[] ranks, int?[] expected)
    {
        ManualScoreInput[] values = Values(points, ranks);
        var result = ManualScoringRules.ResolveFinal(Rules(options), values);
        Assert.Equal(expected, values.Select(player => result[player.PlayerId]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WinnerTie_ContainsOnlyBestScorersAndSupportsOnlyAllowedWinnerCounts(bool ties)
    {
        ScoringRulesSnapshot rules = Rules(new(true, false, true, AllowTies: ties, ResultFromPoints: true, PointsDirection: PointsDirection.Highest));
        ManualScoreInput[] values = Values([10, 10, 5, 5], [1, ties ? 1 : null, null, null]);
        ManualDecision decision = ManualScoringRules.GetDecision(rules, values);
        Assert.Equal(ManualDecisionKind.WinnerTie, decision.Kind);
        Assert.Equal(values.Take(2).Select(player => player.PlayerId), decision.Blocks.Single().PlayerIds);
        Assert.Equal(ties ? 2 : 1, ManualScoringRules.ResolveFinal(rules, values).Count(player => player.Value == 1));
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(rules, values.Select(player => player with { Placement = null }).ToArray()));
        values[2] = values[2] with { Placement = 1 };
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(rules, values));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndependentPointBlocks_StayInsideOriginalPositionsAndUseCompetitionRanks(bool ties)
    {
        ScoringRulesSnapshot rules = Rules(new(true, false, true, true, ties, true, PointsDirection.Highest));
        ManualScoreInput[] values = Values([100, 80, 80, 60, 60, 20], [null, 2, 3, 4, ties ? 4 : 5, null]);
        ManualDecision decision = ManualScoringRules.GetDecision(rules, values);
        Assert.Equal(new[] { 2, 4 }, decision.Blocks.Select(block => block.FirstRank));
        var resolved = ManualScoringRules.ResolveFinal(rules, values);
        Assert.Equal(new int?[] { 1, 2, 3, 4, ties ? 4 : 5, 6 }, values.Select(player => resolved[player.PlayerId]));
        values[1] = values[1] with { Placement = 1 };
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(rules, values));
    }

    [Fact]
    public void ManualPlacements_RejectDenseRanksForbiddenTiesAndUnfinishedResults()
    {
        ScoringRulesSnapshot ties = Rules(new(CaptureWinner: true, CapturePlacements: true, AllowTies: true));
        ManualScoreInput[] values = Values([null, null, null], [1, 1, 3]);
        Assert.Equal(3, ManualScoringRules.ResolveFinal(ties, values).Count);
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(ties, [values[0], values[1], values[2] with { Placement = 2 }]));
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(ties with { AllowTies = false }, values));
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(ties, [values[0], values[1], values[2] with { Placement = null }]));
    }

    [Fact]
    public void Points_NullZeroNegativeAndIntegerExtremesAreDistinct()
    {
        ScoringRulesSnapshot rules = Rules(new(true, true, true, true, ResultFromPoints: true, PointsDirection: PointsDirection.Lowest));
        ManualScoreInput[] values = Values([int.MinValue, 0, int.MaxValue], [null, null, null]);
        Assert.Equal(new int?[] { 1, 2, 3 }, values.Select(player => ManualScoringRules.ResolveFinal(rules, values)[player.PlayerId]));
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(rules with { AllowNegativePoints = false }, values));
        values[0] = values[0] with { Points = null };
        ManualScoringRules.ValidateDraft(rules, values);
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(rules, values));
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(Rules(new()), Values([0], [null])));
        Assert.Throws<ArgumentException>(() => ManualScoringRules.ResolveFinal(Rules(new()), Values([null], [1])));
    }
}

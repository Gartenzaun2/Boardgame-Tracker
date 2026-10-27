using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;

namespace BoardgameTracker.Core.Tests.Games.ArkNova.GameSessions.Scoring;

public sealed class ArkNovaPlacementRulesTests
{
    [Fact]
    public void CreateDefaultPlacements_UsesCompetitionRanking()
    {
        Guid anna = Guid.NewGuid();
        Guid paul = Guid.NewGuid();
        Guid joseph = Guid.NewGuid();
        Guid maria = Guid.NewGuid();
        Dictionary<Guid, int> scores = new()
        {
            [anna] = 108,
            [paul] = 99,
            [joseph] = 99,
            [maria] = 82
        };

        IReadOnlyDictionary<Guid, int> result = ArkNovaPlacementRules.CreateDefaultPlacements(scores);

        Assert.Equal(1, result[anna]);
        Assert.Equal(2, result[paul]);
        Assert.Equal(2, result[joseph]);
        Assert.Equal(4, result[maria]);
    }

    [Fact]
    public void ValidateResolution_AllowsKeepingOrResolvingTie()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Guid third = Guid.NewGuid();
        Dictionary<Guid, int> scores = new() { [first] = 10, [second] = 5, [third] = 5 };

        ArkNovaPlacementRules.ValidateResolution(scores, new Dictionary<Guid, int>
        {
            [first] = 1, [second] = 2, [third] = 2
        });
        ArkNovaPlacementRules.ValidateResolution(scores, new Dictionary<Guid, int>
        {
            [first] = 1, [second] = 2, [third] = 3
        });
    }

    [Fact]
    public void ValidateResolution_RejectsMovingNonTiedPlayer()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Dictionary<Guid, int> scores = new() { [first] = 10, [second] = 5 };

        Assert.Throws<ArgumentException>(() => ArkNovaPlacementRules.ValidateResolution(
            scores,
            new Dictionary<Guid, int> { [first] = 2, [second] = 1 }));
    }

    [Fact]
    public void ValidateResolution_AllowsValidThreeWayTiePartitions()
    {
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        Guid c = Guid.NewGuid();
        Guid winner = Guid.NewGuid();
        Dictionary<Guid, int> scores = new() { [winner] = 10, [a] = 5, [b] = 5, [c] = 5 };

        ArkNovaPlacementRules.ValidateResolution(scores, new Dictionary<Guid, int>
        {
            [winner] = 1, [a] = 2, [b] = 2, [c] = 4
        });
        ArkNovaPlacementRules.ValidateResolution(scores, new Dictionary<Guid, int>
        {
            [winner] = 1, [a] = 2, [b] = 3, [c] = 3
        });
    }
}

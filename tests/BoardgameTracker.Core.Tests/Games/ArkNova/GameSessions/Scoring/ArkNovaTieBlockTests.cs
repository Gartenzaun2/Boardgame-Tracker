using BoardgameTracker.Core.Games.ArkNova.GameSessions.Scoring;

namespace BoardgameTracker.Core.Tests.Games.ArkNova.GameSessions.Scoring;

public sealed class ArkNovaTieBlockTests
{
    [Fact]
    public void TwoIndependentBlocks_ExposeOnlyOwnPlaces_AndChangeIndependently()
    {
        Guid[] ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        int[] values = [120, 120, 110, 100, 100, 90];
        Dictionary<Guid, int> scores = ids.Select((id, index) => (id, score: values[index])).ToDictionary(player => player.id, player => player.score);
        IReadOnlyDictionary<Guid, int> defaults = ArkNovaPlacementRules.CreateDefaultPlacements(scores);
        IReadOnlyList<ArkNovaTieBlock> blocks = ArkNovaTieBlock.Create(scores, defaults);

        Assert.Equal(2, blocks.Count);
        Assert.Equal([ids[0], ids[1]], blocks[0].PlayerIds);
        Assert.Equal([1, 2], blocks[0].AvailablePlacements);
        Assert.Equal([ids[3], ids[4]], blocks[1].PlayerIds);
        Assert.Equal([4, 5], blocks[1].AvailablePlacements);
        Assert.DoesNotContain(blocks, block => block.PlayerIds.Contains(ids[2]) || block.PlayerIds.Contains(ids[5]));
        Assert.Empty(blocks[0].AvailablePlacements.Intersect(blocks[1].AvailablePlacements));

        Assert.Equal(1, blocks[0].Placements[ids[0]]);
        Assert.Equal(1, blocks[0].Placements[ids[1]]);
        blocks[0].SelectPlacement(ids[0], 2);
        Assert.Equal(1, blocks[0].Placements[ids[1]]);
        Assert.Equal(2, blocks[0].Placements[ids[0]]);
        Assert.Equal(4, blocks[1].Placements[ids[3]]);
        Assert.Equal(4, blocks[1].Placements[ids[4]]);

        blocks[1].SelectPlacement(ids[3], 5);
        Assert.Equal(4, blocks[1].Placements[ids[4]]);
        Assert.Equal(5, blocks[1].Placements[ids[3]]);
        Assert.Equal(1, blocks[0].Placements[ids[1]]);
        Assert.Equal(2, blocks[0].Placements[ids[0]]);

        Assert.Throws<ArgumentOutOfRangeException>(() => blocks[0].SelectPlacement(ids[0], 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => blocks[1].SelectPlacement(ids[4], 2));
        Assert.Throws<ArgumentException>(() => blocks[0].SelectPlacement(ids[4], 1));

        Dictionary<Guid, int> resolved = defaults.ToDictionary(player => player.Key, player => player.Value);
        foreach (ArkNovaTieBlock block in blocks)
        {
            foreach (KeyValuePair<Guid, int> placement in block.Placements)
                resolved[placement.Key] = placement.Value;
        }
        Assert.Equal(3, resolved[ids[2]]);
        Assert.Equal(6, resolved[ids[5]]);
        ArkNovaPlacementRules.ValidateResolution(scores, resolved);
    }

    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(0, 2, 1)]
    [InlineData(1, 0, 2)]
    [InlineData(1, 2, 0)]
    [InlineData(2, 0, 1)]
    [InlineData(2, 1, 0)]
    public void ThreePlayerBlock_CanResolveEveryOrder(int first, int second, int third)
    {
        Guid leader = Guid.NewGuid();
        Guid[] tiedPlayers = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        Dictionary<Guid, int> scores = new() { [leader] = 130 };
        foreach (Guid id in tiedPlayers)
            scores[id] = 120;
        ArkNovaTieBlock block = Assert.Single(ArkNovaTieBlock.Create(scores, ArkNovaPlacementRules.CreateDefaultPlacements(scores)));
        Assert.Equal([2, 3, 4], block.AvailablePlacements);

        int[] order = [first, second, third];
        for (int index = 0; index < order.Length; index++)
        {
            block.SelectPlacement(tiedPlayers[order[index]], index + 2);
            Assert.Equal(index + 2, block.Placements[tiedPlayers[order[index]]]);
        }
        Assert.Equal(2, block.Placements[tiedPlayers[first]]);
        Assert.Equal(3, block.Placements[tiedPlayers[second]]);
        Assert.Equal(4, block.Placements[tiedPlayers[third]]);
        Assert.Equal([2, 3, 4], block.Placements.Values.Order());
    }

    [Theory]
    [InlineData(2, 2, 2)]
    [InlineData(2, 2, 4)]
    [InlineData(2, 3, 3)]
    [InlineData(2, 3, 4)]
    public void SharedCompetitionRanking_RemainsAllowed(int a, int b, int c)
    {
        Guid leader = Guid.NewGuid();
        Guid[] ids = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        Dictionary<Guid, int> scores = new() { [leader] = 130, [ids[0]] = 120, [ids[1]] = 120, [ids[2]] = 120 };
        IReadOnlyDictionary<Guid, int> defaults = ArkNovaPlacementRules.CreateDefaultPlacements(scores);
        ArkNovaTieBlock block = Assert.Single(ArkNovaTieBlock.Create(scores, defaults));
        Assert.All(block.Placements.Values, placement => Assert.Equal(2, placement));
        int[] selected = [a, b, c];
        for (int index = 0; index < ids.Length; index++)
            block.SelectPlacement(ids[index], selected[index]);

        Dictionary<Guid, int> resolved = new() { [leader] = 1 };
        foreach (KeyValuePair<Guid, int> player in block.Placements)
            resolved[player.Key] = player.Value;
        ArkNovaPlacementRules.ValidateResolution(scores, resolved);

        block.SelectPlacement(ids[1], 2);
        block.SelectPlacement(ids[2], 2);
        Assert.All(block.Placements.Values, placement => Assert.Equal(2, placement));
    }

    [Fact]
    public void ExistingUniqueAssignment_IsPreserved_AndSelectingItIsANoOp()
    {
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        Dictionary<Guid, int> scores = new() { [a] = 100, [b] = 100 };
        ArkNovaTieBlock block = Assert.Single(ArkNovaTieBlock.Create(scores, new Dictionary<Guid, int> { [a] = 2, [b] = 1 }));
        block.SelectPlacement(b, 1);
        Assert.Equal(1, block.Placements[b]);
        Assert.Equal(2, block.Placements[a]);
    }
}

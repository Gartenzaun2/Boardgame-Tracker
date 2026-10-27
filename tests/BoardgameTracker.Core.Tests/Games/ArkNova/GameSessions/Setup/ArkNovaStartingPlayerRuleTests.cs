using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;

namespace BoardgameTracker.Core.Tests.Games.ArkNova.GameSessions.Setup;

public sealed class ArkNovaStartingPlayerRuleTests
{
    [Fact]
    public void SelectFromPreviousPlacements_ReturnsPlayerWithHighestPlacement()
    {
        Guid anna = Guid.NewGuid();
        Guid ben = Guid.NewGuid();

        Guid? result = ArkNovaStartingPlayerRule.SelectFromPreviousPlacements(
        [
            new(anna, "Anna", 1),
            new(ben, "Ben", 3)
        ]);

        Assert.Equal(ben, result);
    }

    [Fact]
    public void SelectFromPreviousPlacements_WithSharedLastPlace_UsesNameAlphabetically()
    {
        Guid zoe = Guid.NewGuid();
        Guid anna = Guid.NewGuid();
        Guid ben = Guid.NewGuid();

        Guid? result = ArkNovaStartingPlayerRule.SelectFromPreviousPlacements(
        [
            new(zoe, "Zoe", 1),
            new(ben, "Ben", 3),
            new(anna, "Anna", 3)
        ]);

        Assert.Equal(anna, result);
    }

    [Fact]
    public void SelectFromPreviousPlacements_WithoutPlacements_ReturnsNull()
    {
        Assert.Null(ArkNovaStartingPlayerRule.SelectFromPreviousPlacements([]));
    }
}

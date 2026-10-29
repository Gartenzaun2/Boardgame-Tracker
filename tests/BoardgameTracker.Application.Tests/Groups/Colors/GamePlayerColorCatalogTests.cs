using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Tests.Groups.Colors;

public sealed class GamePlayerColorCatalogTests
{
    [Fact]
    public void DifferentGames_UseIndependentSubsetsOfTheSameSemanticColors()
    {
        GamePlayerColorRules other = new("other-game", [PlayerColor.Green, PlayerColor.Purple]);
        GamePlayerColorCatalog catalog = new([ArkNovaPlayerColors.Rules, other]);
        Assert.True(catalog.Get(other.GameKey).Allows(PlayerColor.Green));
        Assert.False(catalog.Get(ArkNovaGame.Key).Allows(PlayerColor.Green));
        Assert.True(catalog.Get(ArkNovaGame.Key).Allows(PlayerColor.Red));
        Assert.False(catalog.Get(other.GameKey).Allows(PlayerColor.Red));
        catalog.Get(other.GameKey).Validate([PlayerColor.Green, null, PlayerColor.Purple]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown-game")]
    [InlineData("without-colors")]
    public void GamesWithoutColors_HaveNoChoicesAndRejectAssignedColors(string? gameKey)
    {
        GamePlayerColorCatalog catalog = new([new GamePlayerColorRules("without-colors", [])]);
        Assert.Empty(catalog.Get(gameKey).AllowedColors);
        catalog.Get(gameKey).Validate([null, null]);
        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.Get(gameKey).Validate([PlayerColor.Red]));
    }
}

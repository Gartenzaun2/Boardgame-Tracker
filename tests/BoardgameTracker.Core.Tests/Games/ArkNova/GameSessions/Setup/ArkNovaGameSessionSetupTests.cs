using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Core.Tests.Games.ArkNova.GameSessions.Setup;

public sealed class ArkNovaGameSessionSetupTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void Validate_WithValidPlayerCount_DoesNotThrow(int playerCount)
    {
        PlayerColor[] colors = [PlayerColor.Blue, PlayerColor.Red, PlayerColor.Yellow, PlayerColor.Black];
        ArkNovaGameSessionSetupPlayer[] players = [.. Enumerable.Range(0, playerCount)
            .Select(index => new ArkNovaGameSessionSetupPlayer(
                Guid.NewGuid(), colors[index], index * 2 + 1))];

        ArkNovaGameSessionSetup.Validate(players);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Validate_WithInvalidPlayerCount_Throws(int playerCount)
    {
        ArkNovaGameSessionSetupPlayer[] players = [.. Enumerable.Range(0, playerCount)
            .Select(index => new ArkNovaGameSessionSetupPlayer(
                Guid.NewGuid(), PlayerColor.Blue, index * 2 + 1))];

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ArkNovaGameSessionSetup.Validate(players));

        Assert.Equal("players", exception.ParamName);
    }

    [Fact]
    public void Validate_WithValidSetup_DoesNotThrow()
    {
        ArkNovaGameSessionSetup.Validate(
        [
            new(Guid.NewGuid(), PlayerColor.Blue, 1),
            new(Guid.NewGuid(), PlayerColor.Red, 4)
        ]);
    }

    [Fact]
    public void Validate_WithDuplicateColors_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ArkNovaGameSessionSetup.Validate(
        [
            new(Guid.NewGuid(), PlayerColor.Blue, 1),
            new(Guid.NewGuid(), PlayerColor.Blue, 3)
        ]));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    public void Validate_WithRepeatedSelectedPlan_DoesNotThrow(int number)
    {
        ArkNovaGameSessionSetup.Validate(
        [
            new(Guid.NewGuid(), PlayerColor.Blue, number),
            new(Guid.NewGuid(), PlayerColor.Red, number)
        ]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(18)]
    public void Validate_WithUnknownSelectedPlan_Throws(int number)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ArkNovaGameSessionSetup.Validate(
            [new(Guid.NewGuid(), PlayerColor.Blue, number)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArkNovaZooPlan(number));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoardgameTracker.Core.Games.ArkNova.GameSessions.ArkNovaGameSessionPlayer(
            Guid.NewGuid(), Guid.NewGuid(), PlayerColor.Blue, number));
    }

    [Fact]
    public void Validate_WithoutSelectedZooPlan_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ArkNovaGameSessionSetup.Validate(
        [
            new(Guid.NewGuid(), PlayerColor.Blue, null)
        ]));
    }

    [Fact]
    public void Validate_WithUndefinedPlayerColor_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ArkNovaGameSessionSetup.Validate(
        [
            new(Guid.NewGuid(), (PlayerColor)999, 1)
        ]));
    }

    [Theory]
    [InlineData(PlayerColor.Green)]
    [InlineData(PlayerColor.White)]
    [InlineData(PlayerColor.Orange)]
    public void Validate_WithGeneralColorOutsideArkNovaPalette_Throws(PlayerColor color)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ArkNovaGameSessionSetup.Validate(
            [new(Guid.NewGuid(), color, 1)]));
    }
}

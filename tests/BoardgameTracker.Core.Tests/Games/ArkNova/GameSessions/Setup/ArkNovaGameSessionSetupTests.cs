using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Core.Tests.Games.ArkNova.GameSessions.Setup;

public sealed class ArkNovaGameSessionSetupTests
{
    [Fact]
    public void Validate_WithValidSetup_DoesNotThrow()
    {
        ArkNovaGameSessionSetup.Validate(
        [
            new(Guid.NewGuid(), PlayerColor.Blue, 1, 2, 1),
            new(Guid.NewGuid(), PlayerColor.Red, 3, 4, 4)
        ]);
    }

    [Fact]
    public void Validate_WithDuplicateColors_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ArkNovaGameSessionSetup.Validate(
        [
            new(Guid.NewGuid(), PlayerColor.Blue, 1, 2, 1),
            new(Guid.NewGuid(), PlayerColor.Blue, 3, 4, 3)
        ]));
    }

    [Fact]
    public void Validate_WithZooPlanOfferedTwice_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ArkNovaGameSessionSetup.Validate(
        [
            new(Guid.NewGuid(), PlayerColor.Blue, 1, 2, 1),
            new(Guid.NewGuid(), PlayerColor.Red, 2, 3, 3)
        ]));
    }

    [Fact]
    public void Validate_WithoutSelectedZooPlan_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ArkNovaGameSessionSetup.Validate(
        [
            new(Guid.NewGuid(), PlayerColor.Blue, 1, 2, null)
        ]));
    }

    [Fact]
    public void Validate_WithUndefinedPlayerColor_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ArkNovaGameSessionSetup.Validate(
        [
            new(Guid.NewGuid(), (PlayerColor)999, 1, 2, 1)
        ]));
    }
}

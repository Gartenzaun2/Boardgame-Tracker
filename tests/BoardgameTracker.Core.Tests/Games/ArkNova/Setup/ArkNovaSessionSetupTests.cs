using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.Setup;

namespace BoardgameTracker.Core.Tests.Games.ArkNova.Setup;

public sealed class ArkNovaSessionSetupTests
{
    [Fact]
    public void Validate_WithValidSetup_DoesNotThrow()
    {
        ArkNovaSessionSetup.Validate(
        [
            new(Guid.NewGuid(), ArkNovaPlayerColor.Blue, 1, 2, 1),
            new(Guid.NewGuid(), ArkNovaPlayerColor.Red, 3, 4, 4)
        ]);
    }

    [Fact]
    public void Validate_WithDuplicateColors_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ArkNovaSessionSetup.Validate(
        [
            new(Guid.NewGuid(), ArkNovaPlayerColor.Blue, 1, 2, 1),
            new(Guid.NewGuid(), ArkNovaPlayerColor.Blue, 3, 4, 3)
        ]));
    }

    [Fact]
    public void Validate_WithZooPlanOfferedTwice_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ArkNovaSessionSetup.Validate(
        [
            new(Guid.NewGuid(), ArkNovaPlayerColor.Blue, 1, 2, 1),
            new(Guid.NewGuid(), ArkNovaPlayerColor.Red, 2, 3, 3)
        ]));
    }

    [Fact]
    public void Validate_WithoutSelectedZooPlan_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ArkNovaSessionSetup.Validate(
        [
            new(Guid.NewGuid(), ArkNovaPlayerColor.Blue, 1, 2, null)
        ]));
    }
}

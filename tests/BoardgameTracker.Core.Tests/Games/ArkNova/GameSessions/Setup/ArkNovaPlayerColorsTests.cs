using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Games.ArkNova.Groups;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Core.Tests.Games.ArkNova.GameSessions.Setup;

public sealed class ArkNovaPlayerColorsTests
{
    [Theory]
    [InlineData(PlayerColor.Green)]
    [InlineData(PlayerColor.White)]
    [InlineData(PlayerColor.Orange)]
    [InlineData(PlayerColor.Brown)]
    [InlineData(PlayerColor.Purple)]
    [InlineData(PlayerColor.Pink)]
    [InlineData(PlayerColor.Turquoise)]
    [InlineData(PlayerColor.Gray)]
    public void KnownGlobalColor_NotAllowedByArkNova_IsRejectedAtAllDomainBoundaries(PlayerColor color)
    {
        var playerId = Guid.NewGuid();
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArkNovaGroupPlayer(Guid.NewGuid(), playerId, color));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArkNovaGameSessionPlayer(Guid.NewGuid(), playerId, color, 1, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ArkNovaGameSessionSetup.Validate(
            [new ArkNovaGameSessionSetupPlayer(playerId, color, 1, 2, 1)]));
    }

    [Theory]
    [InlineData(PlayerColor.Blue, 1)]
    [InlineData(PlayerColor.Yellow, 2)]
    [InlineData(PlayerColor.Red, 3)]
    [InlineData(PlayerColor.Black, 4)]
    public void ArkNovaColors_KeepPersistedNumericMeaning(PlayerColor color, int stored)
    {
        Assert.Equal(stored, (int)color);
        Assert.True(ArkNovaPlayerColors.Rules.Allows(color));
        var group = new ArkNovaGroupPlayer(Guid.NewGuid(), Guid.NewGuid(), color);
        Assert.Equal(color, group.DefaultColor);
    }
}

using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Core.Games.ArkNova;

public static class ArkNovaPlayerColors
{
    public static GamePlayerColorRules Rules { get; } = new(ArkNovaGame.Key,
        [PlayerColor.Blue, PlayerColor.Yellow, PlayerColor.Red, PlayerColor.Black]);
}

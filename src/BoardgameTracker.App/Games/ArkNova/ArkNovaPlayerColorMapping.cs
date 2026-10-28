using BoardgameTracker.Core.Games.ArkNova.Players;
using BoardgameTracker.App.Presentation.PlayerColors;
namespace BoardgameTracker.App.Games.ArkNova;

public static class ArkNovaPlayerColorMapping
{
    public static UiPlayerColor ToUiColor(ArkNovaPlayerColor color) => color switch
    {
        ArkNovaPlayerColor.Blue => UiPlayerColor.Blue,
        ArkNovaPlayerColor.Yellow => UiPlayerColor.Yellow,
        ArkNovaPlayerColor.Red => UiPlayerColor.Red,
        ArkNovaPlayerColor.Black => UiPlayerColor.Black,
        _ => UiPlayerColor.Gray
    };
}

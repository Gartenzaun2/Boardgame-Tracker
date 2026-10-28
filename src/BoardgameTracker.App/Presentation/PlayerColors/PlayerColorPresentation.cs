using BoardgameTracker.App.Resources.Localization;
using Microsoft.Maui.Graphics;
namespace BoardgameTracker.App.Presentation.PlayerColors;

public static class PlayerColorPresentation
{
    public static Color GetBackground(UiPlayerColor color) => Color.FromArgb(PlayerColorPalette.Get(color).BackgroundHex);
    public static Color GetText(UiPlayerColor color) => Color.FromArgb(PlayerColorPalette.Get(color).TextHex);
    public static string GetName(UiPlayerColor color) => color switch
    {
        UiPlayerColor.Red => AppResources.Red,
        UiPlayerColor.Yellow => AppResources.Yellow,
        UiPlayerColor.Green => AppResources.PlayerColorGreen,
        UiPlayerColor.Blue => AppResources.Blue,
        UiPlayerColor.Black => AppResources.Black,
        UiPlayerColor.White => AppResources.PlayerColorWhite,
        UiPlayerColor.Orange => AppResources.PlayerColorOrange,
        UiPlayerColor.Brown => AppResources.PlayerColorBrown,
        UiPlayerColor.Purple => AppResources.PlayerColorPurple,
        UiPlayerColor.Pink => AppResources.PlayerColorPink,
        UiPlayerColor.Turquoise => AppResources.PlayerColorTurquoise,
        UiPlayerColor.Gray => AppResources.PlayerColorGray,
        _ => AppResources.PlayerColorGray
    };
}

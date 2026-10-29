using BoardgameTracker.Core.Players;
using BoardgameTracker.App.Resources.Localization;
using Microsoft.Maui.Graphics;
namespace BoardgameTracker.App.Presentation.PlayerColors;

public static class PlayerColorPresentation
{
    public static Color BlueBackground => GetBackground(PlayerColor.Blue);
    public static Color YellowBackground => GetBackground(PlayerColor.Yellow);
    public static Color RedBackground => GetBackground(PlayerColor.Red);
    public static Color BlackBackground => GetBackground(PlayerColor.Black);
    public static Color GetBackground(PlayerColor color) => Color.FromArgb(PlayerColorPalette.Get(color).BackgroundHex);
    public static Color GetText(PlayerColor color) => Color.FromArgb(PlayerColorPalette.Get(color).TextHex);
    public static string GetName(PlayerColor color) => color switch
    {
        PlayerColor.Red => AppResources.Red,
        PlayerColor.Yellow => AppResources.Yellow,
        PlayerColor.Green => AppResources.PlayerColorGreen,
        PlayerColor.Blue => AppResources.Blue,
        PlayerColor.Black => AppResources.Black,
        PlayerColor.White => AppResources.PlayerColorWhite,
        PlayerColor.Orange => AppResources.PlayerColorOrange,
        PlayerColor.Brown => AppResources.PlayerColorBrown,
        PlayerColor.Purple => AppResources.PlayerColorPurple,
        PlayerColor.Pink => AppResources.PlayerColorPink,
        PlayerColor.Turquoise => AppResources.PlayerColorTurquoise,
        PlayerColor.Gray => AppResources.PlayerColorGray,
        _ => AppResources.PlayerColorGray
    };
}

using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.Maui.Graphics;
using BoardgameTracker.Core.Games.ArkNova.Players;

namespace BoardgameTracker.App.Games.ArkNova;

/// <summary>
/// Centralizes the UI name and tint for Ark Nova player colors. This is presentation
/// logic shared by preparation, group editing and running GameSession screens; the Core
/// color enum remains independent from MAUI and localization.
/// </summary>
public static class ArkNovaPlayerColorPresentation
{
    public static string GetName(ArkNovaPlayerColor color) => color switch
    {
        ArkNovaPlayerColor.Blue => AppResources.Blue,
        ArkNovaPlayerColor.Yellow => AppResources.Yellow,
        ArkNovaPlayerColor.Red => AppResources.Red,
        ArkNovaPlayerColor.Black => AppResources.Black,
        _ => color.ToString()
    };

    public static Color GetTint(ArkNovaPlayerColor color, bool darkBlack = false) => color switch
    {
        ArkNovaPlayerColor.Blue => Color.FromArgb("#8BC4E8"),
        ArkNovaPlayerColor.Yellow => Color.FromArgb("#F2D65C"),
        ArkNovaPlayerColor.Red => Color.FromArgb("#E98A84"),
        ArkNovaPlayerColor.Black => Color.FromArgb(darkBlack ? "#404040" : "#808080"),
        _ => Colors.Transparent
    };
}

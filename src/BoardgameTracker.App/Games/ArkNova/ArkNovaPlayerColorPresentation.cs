using BoardgameTracker.App.Presentation.PlayerColors;
using BoardgameTracker.Core.Games.ArkNova.Players;
using Microsoft.Maui.Graphics;
namespace BoardgameTracker.App.Games.ArkNova;

// Keeps the game enum separate from global display values and localization.
public static class ArkNovaPlayerColorPresentation
{
    public static string GetName(ArkNovaPlayerColor color) => PlayerColorPresentation.GetName(ArkNovaPlayerColorMapping.ToUiColor(color));
    public static Color GetTint(ArkNovaPlayerColor color) => PlayerColorPresentation.GetBackground(ArkNovaPlayerColorMapping.ToUiColor(color));
    public static Color GetText(ArkNovaPlayerColor color) => PlayerColorPresentation.GetText(ArkNovaPlayerColorMapping.ToUiColor(color));
    public static Color BlueBackground => GetTint(ArkNovaPlayerColor.Blue);
    public static Color YellowBackground => GetTint(ArkNovaPlayerColor.Yellow);
    public static Color RedBackground => GetTint(ArkNovaPlayerColor.Red);
    public static Color BlackBackground => GetTint(ArkNovaPlayerColor.Black);
}

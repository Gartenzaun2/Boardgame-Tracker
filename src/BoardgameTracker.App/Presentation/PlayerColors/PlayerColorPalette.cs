using BoardgameTracker.Core.Players;
namespace BoardgameTracker.App.Presentation.PlayerColors;

public sealed record PlayerColorStyle(string BackgroundHex, string TextHex);

// These are display colors, never camera RGB/HSV thresholds.
public static class PlayerColorPalette
{
    public static PlayerColorStyle Get(PlayerColor color) => color switch
    {
        PlayerColor.Red => new("#D96B63", "#312220"),
        PlayerColor.Yellow => new("#E7C84A", "#403716"),
        PlayerColor.Green => new("#73A66F", "#243723"),
        PlayerColor.Blue => new("#6FA9D2", "#253B4B"),
        PlayerColor.Black => new("#555B58", "#E4E9E6"),
        PlayerColor.White => new("#F4F4F1", "#39413D"),
        PlayerColor.Orange => new("#E59A45", "#493117"),
        PlayerColor.Brown => new("#805B42", "#F0E8E2"),
        PlayerColor.Purple => new("#70599E", "#EEEAF4"),
        PlayerColor.Pink => new("#D985AD", "#472838"),
        PlayerColor.Turquoise => new("#6EB7B4", "#264543"),
        PlayerColor.Gray => new("#9AA2A0", "#323936"),
        _ => Get(PlayerColor.Gray)
    };
}

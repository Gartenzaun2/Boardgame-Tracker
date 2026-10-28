namespace BoardgameTracker.App.Presentation.PlayerColors;

// UI categories only. Game-domain enums remain owned by their games.
public enum UiPlayerColor { Red, Yellow, Green, Blue, Black, White, Orange, Brown, Purple, Pink, Turquoise, Gray }
public sealed record PlayerColorStyle(string BackgroundHex, string TextHex);

// These are display colors, never camera RGB/HSV thresholds.
public static class PlayerColorPalette
{
    public static PlayerColorStyle Get(UiPlayerColor color) => color switch
    {
        UiPlayerColor.Red => new("#D96B63", "#312220"),
        UiPlayerColor.Yellow => new("#E7C84A", "#403716"),
        UiPlayerColor.Green => new("#73A66F", "#243723"),
        UiPlayerColor.Blue => new("#6FA9D2", "#253B4B"),
        UiPlayerColor.Black => new("#555B58", "#E4E9E6"),
        UiPlayerColor.White => new("#F4F4F1", "#39413D"),
        UiPlayerColor.Orange => new("#E59A45", "#493117"),
        UiPlayerColor.Brown => new("#805B42", "#F0E8E2"),
        UiPlayerColor.Purple => new("#70599E", "#EEEAF4"),
        UiPlayerColor.Pink => new("#D985AD", "#472838"),
        UiPlayerColor.Turquoise => new("#6EB7B4", "#264543"),
        UiPlayerColor.Gray => new("#9AA2A0", "#323936"),
        _ => Get(UiPlayerColor.Gray)
    };
}

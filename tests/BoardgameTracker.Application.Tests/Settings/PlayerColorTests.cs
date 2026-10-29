using BoardgameTracker.App.Presentation.PlayerColors;
using BoardgameTracker.Core.Players;
namespace BoardgameTracker.Application.Tests.Settings;

public sealed class PlayerColorTests
{
    [Theory]
    [InlineData(PlayerColor.Red, "#D96B63", "#312220")]
    [InlineData(PlayerColor.Yellow, "#E7C84A", "#403716")]
    [InlineData(PlayerColor.Green, "#73A66F", "#243723")]
    [InlineData(PlayerColor.Blue, "#6FA9D2", "#253B4B")]
    [InlineData(PlayerColor.Black, "#555B58", "#E4E9E6")]
    [InlineData(PlayerColor.White, "#F4F4F1", "#39413D")]
    [InlineData(PlayerColor.Orange, "#E59A45", "#493117")]
    [InlineData(PlayerColor.Brown, "#805B42", "#F0E8E2")]
    [InlineData(PlayerColor.Purple, "#70599E", "#EEEAF4")]
    [InlineData(PlayerColor.Pink, "#D985AD", "#472838")]
    [InlineData(PlayerColor.Turquoise, "#6EB7B4", "#264543")]
    [InlineData(PlayerColor.Gray, "#9AA2A0", "#323936")]
    public void Palette_UsesSpecifiedBackgroundAndTextPair(PlayerColor color, string background, string foreground)
    {
        var style = PlayerColorPalette.Get(color);
        Assert.Equal(background, style.BackgroundHex);
        Assert.Equal(foreground, style.TextHex);
        Assert.NotEqual(style.BackgroundHex, style.TextHex);
    }
    [Fact]
    public void AllPlayerColorPairs_MeetNormalTextContrastWithoutRounding()
    {
        static double Luminance(string hex)
        {
            double Channel(int start)
            {
                double value = Convert.ToInt32(hex.Substring(start, 2), 16) / 255.0;
                return value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
            }
            return .2126 * Channel(1) + .7152 * Channel(3) + .0722 * Channel(5);
        }
        foreach (var color in Enum.GetValues<PlayerColor>())
        {
            var pair = PlayerColorPalette.Get(color);
            double background = Luminance(pair.BackgroundHex), text = Luminance(pair.TextHex);
            double contrast = (Math.Max(background, text) + .05) / (Math.Min(background, text) + .05);
            Assert.True(contrast >= 4.5, $"{color}: {contrast:F6}:1");
        }
    }

    [Fact]
    public void Palette_ContainsTwelveSemanticColors_ArkNovaAllowsFour()
    {
        Assert.Equal(12, Enum.GetValues<PlayerColor>().Length);
        Assert.Equal(new[] { PlayerColor.Blue, PlayerColor.Yellow, PlayerColor.Red, PlayerColor.Black },
            BoardgameTracker.Core.Games.ArkNova.ArkNovaPlayerColors.Rules.AllowedColors);
    }
    [Fact]
    public void UnknownValues_UseGrayDisplayFallback()
        => Assert.Equal(PlayerColorPalette.Get(PlayerColor.Gray), PlayerColorPalette.Get((PlayerColor)999));
}

using BoardgameTracker.App.Presentation.PlayerColors;
using BoardgameTracker.App.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.Players;
namespace BoardgameTracker.Application.Tests.Settings;

public sealed class PlayerColorTests
{
    [Theory]
    [InlineData(UiPlayerColor.Red, "#D96B63", "#312220")]
    [InlineData(UiPlayerColor.Yellow, "#E7C84A", "#403716")]
    [InlineData(UiPlayerColor.Green, "#73A66F", "#243723")]
    [InlineData(UiPlayerColor.Blue, "#6FA9D2", "#253B4B")]
    [InlineData(UiPlayerColor.Black, "#555B58", "#E4E9E6")]
    [InlineData(UiPlayerColor.White, "#F4F4F1", "#39413D")]
    [InlineData(UiPlayerColor.Orange, "#E59A45", "#493117")]
    [InlineData(UiPlayerColor.Brown, "#805B42", "#F0E8E2")]
    [InlineData(UiPlayerColor.Purple, "#70599E", "#EEEAF4")]
    [InlineData(UiPlayerColor.Pink, "#D985AD", "#472838")]
    [InlineData(UiPlayerColor.Turquoise, "#6EB7B4", "#264543")]
    [InlineData(UiPlayerColor.Gray, "#9AA2A0", "#323936")]
    public void Palette_UsesSpecifiedBackgroundAndTextPair(UiPlayerColor color, string background, string foreground)
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
        foreach (var color in Enum.GetValues<UiPlayerColor>())
        {
            var pair = PlayerColorPalette.Get(color);
            double background = Luminance(pair.BackgroundHex), text = Luminance(pair.TextHex);
            double contrast = (Math.Max(background, text) + .05) / (Math.Min(background, text) + .05);
            Assert.True(contrast >= 4.5, $"{color}: {contrast:F6}:1");
        }
    }

    [Fact]
    public void Palette_ContainsTwelveUiColors_ArkNovaStillHasFourDomainColors()
    {
        Assert.Equal(12, Enum.GetValues<UiPlayerColor>().Length);
        Assert.Equal(4, Enum.GetValues<ArkNovaPlayerColor>().Length);
    }
    [Theory]
    [InlineData(ArkNovaPlayerColor.Blue, UiPlayerColor.Blue)]
    [InlineData(ArkNovaPlayerColor.Yellow, UiPlayerColor.Yellow)]
    [InlineData(ArkNovaPlayerColor.Red, UiPlayerColor.Red)]
    [InlineData(ArkNovaPlayerColor.Black, UiPlayerColor.Black)]
    public void ArkNovaMapping_UsesGlobalCategory(ArkNovaPlayerColor domain, UiPlayerColor ui)
        => Assert.Equal(ui, ArkNovaPlayerColorMapping.ToUiColor(domain));
    [Fact]
    public void UnknownValues_UseGrayDisplayFallback()
    {
        Assert.Equal(UiPlayerColor.Gray, ArkNovaPlayerColorMapping.ToUiColor((ArkNovaPlayerColor)999));
        Assert.Equal(PlayerColorPalette.Get(UiPlayerColor.Gray), PlayerColorPalette.Get((UiPlayerColor)999));
    }
}

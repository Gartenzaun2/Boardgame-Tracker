using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.App.Theming;
namespace BoardgameTracker.Application.Tests.Settings;

public sealed class ThemeTests
{
    [Theory]
    [InlineData(-1, AppTheme.DeepForest)]
    [InlineData(0, AppTheme.DeepForest)]
    [InlineData(1, AppTheme.RoyalBlue)]
    [InlineData(2, AppTheme.Dark)]
    [InlineData(99, AppTheme.DeepForest)]
    public void StoredSelection_UsesDefaultForUnknownValues(int stored, AppTheme expected)
        => Assert.Equal(expected, AppThemeSelection.FromStoredValue(stored));

    [Fact]
    public void NavigationUpdates_PreserveAppearance()
    {
        Assert.Equal(AppTheme.DeepForest, AppNavigationSettings.Default.Theme);
        var settings = AppNavigationSettings.Default with { Theme = AppTheme.Dark };
        var updated = settings with { LastMainSection = AppMainSection.History, SelectedGameKey = "ark-nova" };
        Assert.Equal(AppTheme.Dark, updated.Theme);
    }

    [Theory]
    [InlineData(AppTheme.DeepForest)]
    [InlineData(AppTheme.RoyalBlue)]
    [InlineData(AppTheme.Dark)]
    public void Palettes_HaveSameRolesAndReadableText(AppTheme theme)
    {
        var palette = ThemePalette.Get(theme);
        Assert.Equal(ThemePalette.Get(AppTheme.DeepForest).Keys.Order(), palette.Keys.Order());
        string[] roles = ["AppBackground", "Surface", "SurfaceSecondary", "Primary", "PrimaryPressed", "PrimaryForeground",
            "SelectedSurface", "SelectedBorder", "TextPrimary", "TextSecondary", "Outline", "NavigationBackground",
            "NavigationText", "NavigationSelectedSurface", "NavigationSelectedText", "Danger", "DangerSurface"];
        Assert.All(roles, role => Assert.True(palette.ContainsKey(role)));
        foreach (var surface in new[] { "AppBackground", "Surface", "SurfaceSecondary", "SelectedSurface" })
            Assert.True(Contrast(palette["TextPrimary"], palette[surface]) >= 4.5, surface);
        Assert.True(Contrast(palette["TextSecondary"], palette["Surface"]) >= 4.5);
        Assert.True(Contrast(palette["NavigationText"], palette["NavigationBackground"]) >= 4.5);
        Assert.True(Contrast(palette["PrimaryForeground"], palette["Primary"]) >= 4.5);
        Assert.True(Contrast(palette["PrimaryForeground"], palette["PrimaryPressed"]) >= 4.5);
        Assert.True(Contrast(palette["Danger"], palette["DangerSurface"]) >= 4.5);
        Assert.True(Contrast(palette["NavigationSelectedText"], palette["NavigationSelectedSurface"]) >= 4.5);
        Assert.True(Contrast(palette["DisabledText"], palette["DisabledSurface"]) >= 4.5);
        Assert.NotEqual(palette["Outline"], palette["Surface"]);
        Assert.True(Contrast(palette["GameGraphicText"], "#FFFFFF") >= 4.5);
    }
    private static double Contrast(string a, string b)
    {
        static double Luminance(string hex)
        {
            double Channel(int start)
            {
                double v = Convert.ToInt32(hex.Substring(start, 2), 16) / 255.0;
                return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4);
            }
            return .2126 * Channel(1) + .7152 * Channel(3) + .0722 * Channel(5);
        }
        double x = Luminance(a), y = Luminance(b);
        return (Math.Max(x, y) + .05) / (Math.Min(x, y) + .05);
    }
}

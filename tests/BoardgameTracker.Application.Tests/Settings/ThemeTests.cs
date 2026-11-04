using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.App.Theming;
namespace BoardgameTracker.Application.Tests.Settings;

public sealed class ThemeTests
{
    [Theory]
    [InlineData(null, AppTheme.Dark)]
    [InlineData(-1, AppTheme.Dark)]
    [InlineData(0, AppTheme.DeepForest)]
    [InlineData(1, AppTheme.RoyalBlue)]
    [InlineData(2, AppTheme.Dark)]
    [InlineData(99, AppTheme.Dark)]
    public void StoredSelection_UsesDefaultForUnknownValues(int? stored, AppTheme expected)
        => Assert.Equal(expected, AppThemeSelection.FromStoredValue(stored));

    [Fact]
    public void NavigationUpdates_PreserveAppearance()
    {
        Assert.Equal(AppTheme.Dark, AppNavigationSettings.Default.Theme);
        var settings = AppNavigationSettings.Default with { Theme = AppTheme.DeepForest };
        var updated = settings with { LastMainSection = AppMainSection.History, SelectedGameKey = "ark-nova" };
        Assert.Equal(AppTheme.DeepForest, updated.Theme);
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
        Assert.NotEqual(palette["Surface"], palette["SurfaceSecondary"]);
        Assert.Equal(palette["Surface"], palette["DisabledSurface"]);
        Assert.Equal(palette["Surface"], palette["NavigationBackground"]);
        Assert.True(Contrast(palette["GameGraphicText"], "#FFFFFF") >= 4.5);
    }
    [Theory]
    [InlineData(AppTheme.DeepForest)]
    [InlineData(AppTheme.RoyalBlue)]
    [InlineData(AppTheme.Dark)]
    public void SegmentStatesUseReadablePairedColorsWithoutDisabledFading(AppTheme theme)
    {
        var document = ReadStyles();
        var style = Assert.Single(document.Descendants(), element => (string?)element.Attribute("{http://schemas.microsoft.com/winfx/2009/xaml}Key") == "SegmentButton");
        Assert.Null(style.Attribute("BasedOn"));
        var selected = Assert.Single(style.Descendants(), element => element.Name.LocalName == "MultiTrigger");
        Assert.Contains(selected.Descendants(), element => (string?)element.Attribute("Property") == "IsSelected" && (string?)element.Attribute("Value") == "True");
        Assert.Contains(selected.Descendants(), element => (string?)element.Attribute("Property") == "IsEnabled" && (string?)element.Attribute("Value") == "True");
        var disabled = Assert.Single(style.Descendants(), element => element.Name.LocalName == "VisualState" && (string?)element.Attribute("{http://schemas.microsoft.com/winfx/2009/xaml}Name") == "Disabled");
        var palette = ThemePalette.Get(theme);
        var normalSetters = style.Elements().Where(element => element.Name.LocalName == "Setter").ToArray();
        var selectedSetters = selected.Elements().Where(element => element.Name.LocalName == "Setter").ToArray();
        var disabledSetters = disabled.Descendants().Where(element => element.Name.LocalName == "Setter").ToArray();
        string Value(IEnumerable<System.Xml.Linq.XElement> setters, string property) => Assert.Single(setters, setter => (string?)setter.Attribute("Property") == property).Attribute("Value")!.Value;
        string Color(IEnumerable<System.Xml.Linq.XElement> setters, string property)
        {
            string token = Value(setters, property);
            Assert.StartsWith("{DynamicResource ", token);
            return palette[token[17..^1]];
        }
        foreach (var state in new[] { normalSetters, selectedSetters, disabledSetters })
            Assert.True(Contrast(Color(state, "TextColor"), Color(state, "BackgroundColor")) >= 4.5);
        Assert.NotEqual(Color(normalSetters, "BackgroundColor"), Color(selectedSetters, "BackgroundColor"));
        Assert.Equal("2", Value(selectedSetters, "BorderWidth"));
        Assert.Equal("1", Value(normalSetters, "BorderWidth"));
        Assert.Equal("1", Value(disabledSetters, "BorderWidth"));
        Assert.Equal("1", Value(disabledSetters, "Opacity"));
        Assert.NotEqual(Color(normalSetters, "BackgroundColor"), Color(disabledSetters, "BackgroundColor"));
        Assert.NotEqual(Color(normalSetters, "TextColor"), Color(disabledSetters, "TextColor"));
        var page = Presentation.AppPageHeaderTests.ReadPage("PlayerListPage");
        var segments = page.Descendants().Where(element => element.Name.LocalName == "SegmentButton").ToArray();
        Assert.Equal(2, segments.Length);
        Assert.All(segments, segment => Assert.Equal("{StaticResource SegmentButton}", (string?)segment.Attribute("Style")));
        Assert.Equal("{Binding HasPlayers}", (string?)segments[1].Attribute("IsEnabled"));
        Assert.All(segments, segment => Assert.Empty(segment.Elements()));
    }

    [Theory]
    [InlineData(AppTheme.DeepForest)]
    [InlineData(AppTheme.RoyalBlue)]
    [InlineData(AppTheme.Dark)]
    public void ActiveSessionImagesUseReadableThemeTintAndKeepTheirOwnSymbol(AppTheme theme)
    {
        foreach (string page in new[] { "GamesPage", "GroupListPage" })
        {
            var document = Presentation.AppPageHeaderTests.ReadPage(page);
            var image = Assert.Single(document.Descendants(), element => (string?)element.Attribute("Source") == "icon_active_session.png");
            Assert.Equal("{DynamicResource TextPrimary}", image.Attributes().Single(attribute => attribute.Name.LocalName == "IconTint.Color").Value);
            var palette = ThemePalette.Get(theme);
            Assert.True(Contrast(palette["TextPrimary"], palette["Surface"]) >= 4.5);
            Assert.NotNull(image.Attribute("IsVisible"));
        }
    }

    [Fact]
    public void HeaderIconStyleSeparatesTwentyFourDpArtworkFromFortyEightDpTouchArea()
    {
        var style = Assert.Single(ReadStyles().Descendants(), element => (string?)element.Attribute("{http://schemas.microsoft.com/winfx/2009/xaml}Key") == "HeaderIconButton");
        string Value(string property) => Assert.Single(style.Elements(), element => (string?)element.Attribute("Property") == property).Attribute("Value")!.Value;
        Assert.Equal("{StaticResource IconButton}", (string?)style.Attribute("BasedOn"));
        Assert.Equal("48", Value("WidthRequest"));
        Assert.Equal("48", Value("HeightRequest"));
        Assert.Equal("12", Value("Padding"));
        Assert.Equal("AspectFit", Value("Aspect"));
        Assert.Contains("{StaticResource HeaderIconButton}", Presentation.AppPageHeaderTests.ReadHeaderText());
    }

    [Theory]
    [InlineData(AppTheme.DeepForest, "#176B80")]
    [InlineData(AppTheme.RoyalBlue, "#704089")]
    [InlineData(AppTheme.Dark, "#678AB8")]
    public void GameContextUsesIndependentMatchingTextAndBorderAccent(AppTheme theme, string accent)
    {
        var palette = ThemePalette.Get(theme);
        Assert.Equal(accent, palette["GameContextText"]);
        Assert.Equal(accent, palette["GameContextBorder"]);
        Assert.NotEqual(palette["Primary"], accent);
        Assert.NotEqual(palette["SelectedBorder"], accent);
        Assert.NotEqual(palette["NavigationSelectedText"], accent);
    }

    private static System.Xml.Linq.XDocument ReadStyles()
    {
        using StreamReader reader = new(typeof(ThemeTests).Assembly.GetManifestResourceStream("ThemeContract.App.xaml")!);
        return System.Xml.Linq.XDocument.Parse(reader.ReadToEnd());
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

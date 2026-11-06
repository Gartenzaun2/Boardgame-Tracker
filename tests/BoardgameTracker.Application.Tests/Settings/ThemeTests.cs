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
    [InlineData(3, AppTheme.DarkTfm)]
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
    [InlineData(AppTheme.DarkTfm)]
    public void Palettes_HaveSameRolesAndReadableText(AppTheme theme)
    {
        var palette = ThemePalette.Get(theme);
        Assert.Equal(ThemePalette.Get(AppTheme.DeepForest).Keys.Order(), palette.Keys.Order());
        string[] roles = ["AppBackground", "Surface", "CardSurface", "CardOutline", "SurfaceSecondary", "Primary", "PrimaryPressed", "PrimaryForeground",
            "SelectedSurface", "SelectedBorder", "TextPrimary", "TextSecondary", "Outline", "NavigationBackground",
            "NavigationText", "NavigationSelectedSurface", "NavigationSelectedText", "Danger", "DangerSurface",
            "DisabledSurface", "DisabledText", "RecognitionHigh", "RecognitionMedium", "GameContextText", "GameContextBorder",
            "IconSurface", "GameGraphicText", "ModalOverlay", "BusyOverlay", "BusyIndicator"];
        Assert.Equal(roles.Order(), palette.Keys.Order());
        foreach (var surface in new[] { "AppBackground", "Surface", "CardSurface", "SurfaceSecondary", "SelectedSurface" })
            Assert.True(Contrast(palette["TextPrimary"], palette[surface]) >= 4.5, surface);
        Assert.True(Contrast(palette["TextSecondary"], palette["Surface"]) >= 4.5);
        Assert.True(Contrast(palette["TextSecondary"], palette["CardSurface"]) >= 4.5);
        Assert.True(Contrast(palette["NavigationText"], palette["NavigationBackground"]) >= 4.5);
        Assert.True(Contrast(palette["PrimaryForeground"], palette["Primary"]) >= 4.5);
        Assert.True(Contrast(palette["PrimaryForeground"], palette["PrimaryPressed"]) >= 4.5);
        Assert.True(Contrast(palette["Danger"], palette["DangerSurface"]) >= 4.5);
        Assert.True(Contrast(palette["NavigationSelectedText"], palette["NavigationSelectedSurface"]) >= 4.5);
        Assert.True(Contrast(palette["DisabledText"], palette["DisabledSurface"]) >= 4.5);
        Assert.NotEqual(palette["Outline"], palette["Surface"]);
        Assert.NotEqual(palette["Surface"], palette["SurfaceSecondary"]);
        Assert.NotEqual(palette["CardSurface"], palette["CardOutline"]);
        if (theme == AppTheme.Dark)
        {
            Assert.Equal("#222A26", palette["DisabledSurface"]);
            Assert.NotEqual(palette["Surface"], palette["DisabledSurface"]);
            Assert.NotEqual(palette["Surface"], palette["NavigationBackground"]);
            Assert.NotEqual(palette["CardSurface"], palette["NavigationBackground"]);
        }
        else
        {
            if (theme == AppTheme.DeepForest)
                Assert.Equal("#DAE4DF", palette["DisabledSurface"]);
            else
                Assert.Equal(palette["Surface"], palette["DisabledSurface"]);
            if (theme == AppTheme.DarkTfm)
                Assert.NotEqual(palette["Surface"], palette["NavigationBackground"]);
            else
                Assert.Equal(palette["Surface"], palette["NavigationBackground"]);
        }
        Assert.True(Contrast(palette["GameGraphicText"], "#FFFFFF") >= 4.5);
    }
    [Theory]
    [InlineData(AppTheme.DeepForest)]
    [InlineData(AppTheme.RoyalBlue)]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.DarkTfm)]
    public void SegmentStatesUseReadablePairedColorsWithoutDisabledFading(AppTheme theme)
    {
        var document = ReadStyles();
        var style = Assert.Single(document.Descendants(), element => (string?)element.Attribute("{http://schemas.microsoft.com/winfx/2009/xaml}Key") == "SegmentButton");
        Assert.Null(style.Attribute("BasedOn"));
        Assert.DoesNotContain(style.Descendants(), element => element.Name.LocalName is "MultiTrigger" or "Trigger" or "DataTrigger");
        var states = style.Descendants().Where(element => element.Name.LocalName == "VisualState").ToArray();
        Assert.Equal(new[] { "Normal", "Selected", "Disabled" }, states.Select(state => (string?)state.Attribute("{http://schemas.microsoft.com/winfx/2009/xaml}Name")));
        System.Xml.Linq.XElement State(string name) => Assert.Single(states, element => (string?)element.Attribute("{http://schemas.microsoft.com/winfx/2009/xaml}Name") == name);
        var palette = ThemePalette.Get(theme);
        var normalSetters = State("Normal").Descendants().Where(element => element.Name.LocalName == "Setter").ToArray();
        var selectedSetters = State("Selected").Descendants().Where(element => element.Name.LocalName == "Setter").ToArray();
        var disabledSetters = State("Disabled").Descendants().Where(element => element.Name.LocalName == "Setter").ToArray();
        string Value(IEnumerable<System.Xml.Linq.XElement> setters, string property) => Assert.Single(setters, setter => (string?)setter.Attribute("Property") == property).Attribute("Value")!.Value;
        string Color(IEnumerable<System.Xml.Linq.XElement> setters, string property)
        {
            string token = Value(setters, property);
            Assert.StartsWith("{DynamicResource ", token);
            return palette[token[17..^1]];
        }
        foreach (var state in new[] { normalSetters, selectedSetters, disabledSetters })
        {
            Assert.Equal(new[] { "BackgroundColor", "BorderColor", "BorderWidth", "Opacity", "TextColor" }, state.Select(setter => (string?)setter.Attribute("Property")).Order());
            Assert.StartsWith("{DynamicResource ", Value(state, "BorderColor"));
            Assert.True(Contrast(Color(state, "TextColor"), Color(state, "BackgroundColor")) >= 4.5);
            Assert.Equal("1", Value(state, "Opacity"));
        }
        Assert.Equal("{DynamicResource SurfaceSecondary}", Value(normalSetters, "BackgroundColor"));
        Assert.Equal("{DynamicResource TextPrimary}", Value(normalSetters, "TextColor"));
        Assert.Equal("{DynamicResource Outline}", Value(normalSetters, "BorderColor"));
        Assert.Equal("{DynamicResource SelectedSurface}", Value(selectedSetters, "BackgroundColor"));
        Assert.Equal("{DynamicResource TextPrimary}", Value(selectedSetters, "TextColor"));
        Assert.Equal("{DynamicResource SelectedBorder}", Value(selectedSetters, "BorderColor"));
        Assert.Equal("{DynamicResource Surface}", Value(disabledSetters, "BackgroundColor"));
        Assert.Equal("{DynamicResource TextSecondary}", Value(disabledSetters, "TextColor"));
        Assert.Equal("{DynamicResource Outline}", Value(disabledSetters, "BorderColor"));
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

    [Fact]
    public void SegmentStateChangesUseOnePathWithDisabledPriorityAndRetainSelectionBindings()
    {
        using StreamReader reader = new(typeof(ThemeTests).Assembly.GetManifestResourceStream("ThemeContract.SegmentButton.cs")!);
        string source = reader.ReadToEnd();
        Assert.Contains("propertyChanged: (bindable, _, _) => ((SegmentButton)bindable).ChangeVisualState()", source);
        Assert.Contains("protected override void ChangeVisualState()", source);
        Assert.Contains("!IsEnabled ? VisualStateManager.CommonStates.Disabled", source);
        Assert.Contains(": IsSelected ? VisualStateManager.CommonStates.Selected", source);
        Assert.Contains(": VisualStateManager.CommonStates.Normal", source);
        Assert.Contains("VisualStateManager.GoToState(this, state)", source);
        Assert.DoesNotContain("base.ChangeVisualState()", source);
        var page = Presentation.AppPageHeaderTests.ReadPage("PlayerListPage");
        var segments = page.Descendants().Where(element => element.Name.LocalName == "SegmentButton").ToArray();
        Assert.Equal(new[] { "{Binding ShowPlayers}", "{Binding ShowGroups}" }, segments.Select(element => (string?)element.Attribute("IsSelected")));
        Assert.Contains(page.Descendants(), element => (string?)element.Attribute("IsEnabled") == "{Binding IsIdle}");
    }

    [Theory]
    [InlineData(AppTheme.DeepForest)]
    [InlineData(AppTheme.RoyalBlue)]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.DarkTfm)]
    public void ActiveSessionImagesUseReadableThemeTintAndKeepTheirOwnSymbol(AppTheme theme)
    {
        foreach (string page in new[] { "GamesPage", "GroupListPage" })
        {
            var document = Presentation.AppPageHeaderTests.ReadPage(page);
            var image = Assert.Single(document.Descendants(), element => (string?)element.Attribute("Source") == "icon_active_session.png");
            Assert.Equal("{DynamicResource TextPrimary}", image.Attributes().Single(attribute => attribute.Name.LocalName == "IconTint.Color").Value);
            var palette = ThemePalette.Get(theme);
            Assert.True(Contrast(palette["TextPrimary"], palette["CardSurface"]) >= 4.5);
            Assert.NotNull(image.Attribute("IsVisible"));
        }
    }

    [Fact]
    public void HeaderIconStyleSeparatesTwentyFourDpArtworkFromFortyFourDpTouchArea()
    {
        var style = Assert.Single(ReadStyles().Descendants(), element => (string?)element.Attribute("{http://schemas.microsoft.com/winfx/2009/xaml}Key") == "HeaderIconButton");
        string Value(string property) => Assert.Single(style.Elements(), element => (string?)element.Attribute("Property") == property).Attribute("Value")!.Value;
        Assert.Equal("{StaticResource IconButton}", (string?)style.Attribute("BasedOn"));
        Assert.Equal("44", Value("WidthRequest"));
        Assert.Equal("44", Value("HeightRequest"));
        Assert.Equal("10", Value("Padding"));
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

    [Theory]
    [InlineData("AppBackground", "#101A1B")]
    [InlineData("Surface", "#1B2B30")]
    [InlineData("CardSurface", "#263D49")]
    [InlineData("CardOutline", "#426070")]
    [InlineData("SurfaceSecondary", "#213B37")]
    [InlineData("Primary", "#70A98E")]
    [InlineData("PrimaryPressed", "#61977E")]
    [InlineData("PrimaryForeground", "#10251C")]
    [InlineData("SelectedSurface", "#34594E")]
    [InlineData("SelectedBorder", "#729B87")]
    [InlineData("TextPrimary", "#F0F5F6")]
    [InlineData("TextSecondary", "#C0D0D7")]
    [InlineData("Outline", "#354A49")]
    [InlineData("NavigationBackground", "#172326")]
    [InlineData("NavigationText", "#AEBBB4")]
    [InlineData("NavigationSelectedSurface", "#34594E")]
    [InlineData("NavigationSelectedText", "#F0F5F6")]
    public void DarkUsesApprovedMidnightColors(string role, string expected)
        => Assert.Equal(expected, ThemePalette.Get(AppTheme.Dark)[role]);

    [Theory]
    [InlineData("Danger", "#FFB4AB")]
    [InlineData("DangerSurface", "#5F2423")]
    [InlineData("RecognitionHigh", "#8CD9A3")]
    [InlineData("RecognitionMedium", "#F5D45E")]
    [InlineData("GameContextText", "#678AB8")]
    [InlineData("GameContextBorder", "#678AB8")]
    [InlineData("IconSurface", "#EEF5F1")]
    [InlineData("GameGraphicText", "#101713")]
    [InlineData("ModalOverlay", "#66000000")]
    [InlineData("BusyOverlay", "#99000000")]
    [InlineData("BusyIndicator", "#FFFFFF")]
    [InlineData("DisabledSurface", "#222A26")]
    [InlineData("DisabledText", "#F0F4F1")]
    public void DarkPreservesExistingSpecialRoles(string role, string expected)
        => Assert.Equal(expected, ThemePalette.Get(AppTheme.Dark)[role]);

    [Theory]
    [InlineData(AppTheme.DeepForest, "#B8CAC0")]
    [InlineData(AppTheme.RoyalBlue, "#BCCAE1")]
    public void LightCardsRetainTheirOriginalSurfacesAndOutlines(AppTheme theme, string outline)
    {
        var palette = ThemePalette.Get(theme);
        if (theme == AppTheme.DeepForest)
        {
            Assert.Equal("#F7FBF9", palette["Surface"]);
            Assert.Equal("#D5ECF2", palette["CardSurface"]);
            Assert.Equal("#78B0BE", palette["CardOutline"]);
            Assert.NotEqual(palette["CardSurface"], palette["NavigationBackground"]);
        }
        else
        {
            Assert.Equal("#FFFFFF", palette["Surface"]);
            Assert.Equal(outline, palette["Outline"]);
            Assert.Equal(palette["Surface"], palette["CardSurface"]);
            Assert.Equal(palette["Outline"], palette["CardOutline"]);
        }
    }

    [Fact]
    public void ThemeIdentityAndCentralCardResourcesRemainSemantic()
    {
        Assert.Equal(4, Enum.GetValues<AppTheme>().Length);
        Assert.Equal(3, (int)AppTheme.DarkTfm);
        Assert.Equal(2, (int)AppTheme.Dark);
        Assert.Equal(AppTheme.Dark, AppNavigationSettings.Default.Theme);
        Assert.Equal("Arche Nova (Dunkel)", BoardgameTracker.App.Resources.Localization.AppResources.ThemeDark);
        var styles = ReadStyles();
        var card = Assert.Single(styles.Descendants(), element => (string?)element.Attribute("{http://schemas.microsoft.com/winfx/2009/xaml}Key") == "CardBorder");
        Assert.Contains(card.Elements(), element => (string?)element.Attribute("Property") == "BackgroundColor" && (string?)element.Attribute("Value") == "{DynamicResource CardSurface}");
        Assert.Contains(card.Elements(), element => (string?)element.Attribute("Property") == "Stroke" && (string?)element.Attribute("Value") == "{DynamicResource CardOutline}");
        var dialog = Assert.Single(styles.Descendants(), element => (string?)element.Attribute("{http://schemas.microsoft.com/winfx/2009/xaml}Key") == "SelectionDialog");
        Assert.Contains(dialog.Elements(), element => (string?)element.Attribute("Property") == "BackgroundColor" && (string?)element.Attribute("Value") == "{DynamicResource AppBackground}");
        Assert.Contains(dialog.Elements(), element => (string?)element.Attribute("Property") == "Stroke" && (string?)element.Attribute("Value") == "{DynamicResource Outline}");
    }

    [Theory]
    [InlineData("AppBackground", "#141214")]
    [InlineData("Surface", "#231C1D")]
    [InlineData("CardSurface", "#482A2B")]
    [InlineData("CardOutline", "#80504A")]
    [InlineData("SurfaceSecondary", "#2B2223")]
    [InlineData("Primary", "#B97849")]
    [InlineData("PrimaryPressed", "#AF7047")]
    [InlineData("PrimaryForeground", "#1C130F")]
    [InlineData("SelectedSurface", "#5A3332")]
    [InlineData("SelectedBorder", "#D09464")]
    [InlineData("TextPrimary", "#F5EEE8")]
    [InlineData("TextSecondary", "#D3BEB5")]
    [InlineData("Outline", "#4C3A36")]
    [InlineData("NavigationBackground", "#1A1516")]
    [InlineData("NavigationText", "#B8AAA2")]
    [InlineData("NavigationSelectedSurface", "#5A3332")]
    [InlineData("NavigationSelectedText", "#FFF7F1")]
    [InlineData("RecognitionHigh", "#8CD9A3")]
    [InlineData("RecognitionMedium", "#F5D45E")]
    [InlineData("Danger", "#FFB4AB")]
    [InlineData("DangerSurface", "#5F2423")]
    [InlineData("DisabledSurface", "#231C1D")]
    [InlineData("DisabledText", "#D3BEB5")]
    [InlineData("ModalOverlay", "#66000000")]
    [InlineData("BusyOverlay", "#99000000")]
    [InlineData("IconSurface", "#EEF5F1")]
    [InlineData("GameGraphicText", "#101713")]
    [InlineData("BusyIndicator", "#FFFFFF")]
    [InlineData("GameContextText", "#D8AA87")]
    [InlineData("GameContextBorder", "#D8AA87")]
    public void TfmUsesApprovedPalette(string role, string color) => Assert.Equal(color, ThemePalette.Get(AppTheme.DarkTfm)[role]);

    [Theory]
    [InlineData("AppBackground", "#E5F2EC")]
    [InlineData("Surface", "#F7FBF9")]
    [InlineData("CardSurface", "#D5ECF2")]
    [InlineData("CardOutline", "#78B0BE")]
    [InlineData("SurfaceSecondary", "#E5F0DA")]
    [InlineData("Primary", "#427C35")]
    [InlineData("PrimaryPressed", "#326427")]
    [InlineData("PrimaryForeground", "#FFFFFF")]
    [InlineData("SelectedSurface", "#D6EABC")]
    [InlineData("SelectedBorder", "#6F9C4E")]
    [InlineData("TextPrimary", "#263D32")]
    [InlineData("TextSecondary", "#4F696B")]
    [InlineData("Outline", "#BBD4CE")]
    [InlineData("NavigationBackground", "#F7FBF9")]
    [InlineData("NavigationText", "#4F696B")]
    [InlineData("NavigationSelectedSurface", "#DCECC7")]
    [InlineData("NavigationSelectedText", "#263D32")]
    [InlineData("DisabledSurface", "#DAE4DF")]
    [InlineData("DisabledText", "#4F696B")]
    [InlineData("GameContextText", "#176B80")]
    [InlineData("GameContextBorder", "#176B80")]
    public void LightArcheNovaUsesApprovedPalette(string role, string color) => Assert.Equal(color, ThemePalette.Get(AppTheme.DeepForest)[role]);

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

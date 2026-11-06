using System.Reflection;
using System.Xml.Linq;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class UxConsolidationTests
{
    private static string Resource(string name)
    {
        using StreamReader reader = new(typeof(UxConsolidationTests).Assembly.GetManifestResourceStream("UxConsolidation." + name)!);
        return reader.ReadToEnd();
    }

    [Fact]
    public void ScoringNavigationAndCardSelectionHaveDisjointTouchSurfaces()
    {
        var page = AppPageHeaderTests.ReadPage("ArkNovaScoringPage");
        var playerTap = Assert.Single(page.Descendants(), element => (string?)element.Attribute("Tapped") == "OnPlayerTapped");
        var cardTap = Assert.Single(page.Descendants(), element => (string?)element.Attribute("Tapped") == "OnAddSpecialCardTapped");
        var playerSurface = playerTap.Parent!.Parent!;
        var cardSurface = cardTap.Parent!.Parent!;
        Assert.Same(playerSurface.Parent, cardSurface.Parent);
        Assert.NotSame(playerSurface, cardSurface);
        Assert.Equal("1", (string?)cardSurface.Attribute("Grid.Column"));
        Assert.Contains(playerSurface.Descendants(), element => (string?)element.Attribute("Source") == "icon_chevron.png");
        Assert.DoesNotContain(cardSurface.Descendants(), element => (string?)element.Attribute("Source") == "icon_chevron.png");
        var border = Assert.Single(cardSurface.Elements(), element => element.Name.LocalName == "Border");
        Assert.Equal("Transparent", (string?)border.Attribute("BackgroundColor"));
        var icon = Assert.Single(border.Elements());
        Assert.Equal("icon_special_card.png", (string?)icon.Attribute("Source"));
        Assert.DoesNotContain(icon.Attributes(), attribute => attribute.Name.LocalName == "IconTint.Color");
        var svg = XDocument.Parse(Resource("icon_special_card.svg"));
        Assert.Equal(3, svg.Descendants().Count(element => element.Name.LocalName == "rect" && (string?)element.Attribute("fill") == "#FFFFFF"));
    }

    [Fact]
    public void SettingsRootIsFlatAndUsesTheSameLookupStyleWithDirectLicenseNavigation()
    {
        var page = AppPageHeaderTests.ReadPage("AppSettingsPage");
        Assert.All(page.Descendants().Where(element => element.Name.LocalName == "Border"),
            border => Assert.Equal("{StaticResource SettingsLookupFrame}", (string?)border.Attribute("Style")));
        var headings = page.Descendants().Where(element => (string?)element.Attribute("Style") == "{StaticResource SettingsSectionTitle}");
        Assert.Equal(new[] { "{x:Static res:AppResources.SettingsGeneral}", "{x:Static res:AppResources.GamesSection}", "{x:Static res:AppResources.AboutLegal}" }, headings.Select(element => (string?)element.Attribute("Text")));
        var choices = page.Descendants().Where(element => (string?)element.Attribute("Style") == "{StaticResource SettingsLookup}");
        Assert.Equal(new[] { "OnLanguageClicked", "OnThemeClicked" }, choices.Select(element => (string?)element.Attribute("Clicked")));
        Assert.Contains(page.Descendants(), element => (string?)element.Attribute("Tapped") == "OnLicensesClicked");
        var games = AppPageHeaderTests.ReadPage("MyGamesPage");
        Assert.DoesNotContain(games.Descendants(), element => element.Name.LocalName == "Switch");
        Assert.Contains(games.Descendants(), element => (string?)element.Attribute("Tapped") == "OnGameTapped");
    }

    [Fact]
    public void GameSettingsAreFlatAndRetainTheirBindingsInfoAndNavigationActions()
    {
        var page = AppPageHeaderTests.ReadPage("ArkNovaSettingsPage");
        Assert.All(page.Descendants().Where(element => element.Name.LocalName == "Border"),
            border => Assert.Equal("{StaticResource SettingsLookupFrame}", (string?)border.Attribute("Style")));
        Assert.Equal(new[] { "{Binding IsEnabled, Mode=OneWay}", "{Binding CaptureReputation}", "{Binding UseOldScoring}" },
            page.Descendants().Where(element => element.Name.LocalName == "Switch").Select(element => (string?)element.Attribute("IsToggled")));
        Assert.Contains(page.Descendants(), element => (string?)element.Attribute("Clicked") == "OnMaterialInfoClicked");
        var material = Assert.Single(page.Descendants(), element => (string?)element.Attribute("Clicked") == "OnContentLanguageClicked");
        Assert.Equal("{Binding SelectedContentLanguage.Name}", (string?)material.Attribute("Text"));
        Assert.Equal("{StaticResource SettingsLookup}", (string?)material.Attribute("Style"));
        foreach (string action in new[] { "OnSpecialCardsClicked", "OnPhotoDatasetClicked" })
        {
            var tap = Assert.Single(page.Descendants(), element => (string?)element.Attribute("Tapped") == action);
            Assert.Contains(tap.Parent!.Parent!.Elements(), element => (string?)element.Attribute("Source") == "icon_chevron.png");
        }
        foreach (string screen in new[] { "AppSettingsPage", "ArkNovaSettingsPage" })
        {
            var document = AppPageHeaderTests.ReadPage(screen);
            Assert.Contains(document.Descendants(), element => (string?)element.Attribute("Style") == "{StaticResource SettingsPageBody}");
            Assert.Contains(document.Descendants(), element => (string?)element.Attribute("Style") == "{StaticResource SettingsContent}");
        }
    }

    [Fact]
    public void LookupArrowIsPartOfTheSharedButtonRatherThanASeparateTouchTarget()
    {
        using StreamReader reader = new(typeof(UxConsolidationTests).Assembly.GetManifestResourceStream("ThemeContract.App.xaml")!);
        var styles = XDocument.Parse(reader.ReadToEnd());
        XNamespace x = "http://schemas.microsoft.com/winfx/2009/xaml";
        var lookup = Assert.Single(styles.Descendants(), element => (string?)element.Attribute(x + "Key") == "SettingsLookup");
        Assert.Equal("Button", (string?)lookup.Attribute("TargetType"));
        Assert.Contains(lookup.Elements(), setter => (string?)setter.Attribute("Property") == "ImageSource" && (string?)setter.Attribute("Value") == "icon_lookup_down.png");
        Assert.Contains(lookup.Elements(), setter => (string?)setter.Attribute("Property") == "ContentLayout" && (string?)setter.Attribute("Value") == "Right,4");
        foreach (string screen in new[] { "AppSettingsPage", "ArkNovaSettingsPage" })
        {
            var page = AppPageHeaderTests.ReadPage(screen);
            Assert.DoesNotContain(page.Descendants(), element => (string?)element.Attribute("Source") == "icon_lookup_down.png");
        }
    }

    [Fact]
    public void CompactLookupKeepsOneAccessibleFortyFourDpTargetAndAllowsTextToGrow()
    {
        using StreamReader reader = new(typeof(UxConsolidationTests).Assembly.GetManifestResourceStream("ThemeContract.App.xaml")!);
        var document = XDocument.Parse(reader.ReadToEnd());
        XNamespace x = "http://schemas.microsoft.com/winfx/2009/xaml";
        XElement Style(string key) => Assert.Single(document.Descendants(), element => (string?)element.Attribute(x + "Key") == key);
        string Value(string key, string property) => Assert.Single(Style(key).Elements(), element => (string?)element.Attribute("Property") == property).Attribute("Value")!.Value;
        Assert.Equal("21", Value("SettingsSectionTitle", "FontSize"));
        Assert.Equal("16", Value("SettingsItemLabel", "FontSize"));
        Assert.Equal("{DynamicResource TextPrimary}", Value("SettingsItemLabel", "TextColor"));
        Assert.Equal("13", Value("SettingsHintLabel", "FontSize"));
        Assert.Equal("15", Value("SettingsLookup", "FontSize"));
        Assert.Equal("132", Value("SettingsLookupHost", "WidthRequest"));
        Assert.Equal("44", Value("SettingsLookupHost", "MinimumHeightRequest"));
        Assert.Equal("Center", Value("SettingsLookupHost", "VerticalOptions"));
        Assert.Equal("44", Value("SettingsLookup", "MinimumHeightRequest"));
        Assert.Equal("40", Value("SettingsLookupFrame", "MinimumHeightRequest"));
        Assert.Equal("0,2", Value("SettingsLookupFrame", "Margin"));
        Assert.Equal("True", Value("SettingsLookupFrame", "InputTransparent"));
        Assert.Equal("False", Value("SettingsLookupFrame", "AutomationProperties.IsInAccessibleTree"));
        Assert.Equal("Transparent", Value("SettingsLookup", "BackgroundColor"));
        Assert.Equal("0", Value("SettingsLookup", "BorderWidth"));
        Assert.Equal("WordWrap", Value("SettingsLookup", "LineBreakMode"));
        Assert.DoesNotContain(Style("SettingsLookup").Elements(), setter => (string?)setter.Attribute("Property") is "HeightRequest" or "MaximumHeightRequest");
        foreach (string screen in new[] { "AppSettingsPage", "ArkNovaSettingsPage" })
        {
            var page = AppPageHeaderTests.ReadPage(screen);
            var buttons = page.Descendants().Where(element => (string?)element.Attribute("Style") == "{StaticResource SettingsLookup}").ToArray();
            Assert.Equal(screen == "AppSettingsPage" ? 2 : 1, buttons.Length);
            foreach (var button in buttons)
            {
                Assert.Equal("{StaticResource SettingsLookupHost}", (string?)button.Parent!.Attribute("Style"));
                Assert.Equal(2, button.Parent.Elements().Count());
                Assert.NotNull(button.Attribute("Clicked"));
                Assert.NotNull(button.Attribute("SemanticProperties.Description"));
                Assert.DoesNotContain(button.Parent.Descendants(), element => element.Name.LocalName == "TapGestureRecognizer");
            }
        }
    }

    [Theory]
    [InlineData("CaptureReputation", "CaptureReputationHint", "OnCaptureReputationInfoClicked")]
    [InlineData("UseOldScoring", "OldScoringDisplayHint", "OnOldScoringInfoClicked")]
    public void ExplanationIsAvailableViaInfoBesideItsSwitchInsteadOfPermanentText(string setting, string hint, string action)
    {
        var page = AppPageHeaderTests.ReadPage("ArkNovaSettingsPage");
        var info = Assert.Single(page.Descendants(), element => (string?)element.Attribute("Clicked") == action);
        Assert.Equal("ImageButton", info.Name.LocalName);
        Assert.Equal("icon_info.png", (string?)info.Attribute("Source"));
        Assert.Equal("{StaticResource IconButton}", (string?)info.Attribute("Style"));
        var toggle = Assert.Single(info.Parent!.Elements(), element => element.Name.LocalName == "Switch");
        Assert.Equal("{Binding " + setting + "}", (string?)toggle.Attribute("IsToggled"));
        Assert.NotEqual((string?)info.Attribute("Grid.Column"), (string?)toggle.Attribute("Grid.Column"));
        Assert.DoesNotContain(page.Descendants(), element => element.Name.LocalName == "Label" && (string?)element.Attribute("Text") == "{x:Static res:AppResources." + hint + "}");
        var locked = Assert.Single(page.Descendants(), element => (string?)element.Attribute("Text") == "{x:Static res:AppResources.GameDisableBlocked}");
        Assert.Equal("{Binding IsLocked}", (string?)locked.Attribute("IsVisible"));
        var assembly = typeof(UxConsolidationTests).Assembly;
        string resource = Assert.Single(assembly.GetManifestResourceNames(), name => name.EndsWith("ArkNovaSettingsPage.xaml.cs", StringComparison.Ordinal));
        using StreamReader source = new(assembly.GetManifestResourceStream(resource)!);
        Assert.Contains("AppResources." + hint + ", AppResources.Ok", source.ReadToEnd());
    }

    [Fact]
    public void HistoryVisibilityDoesNotFilterTheTransferSnapshot()
    {
        string source = Resource("HistoryPage.xaml.cs");
        Assert.Contains("_exportSessions = await _query.ListAsync();", source);
        Assert.Contains("_all = _exportSessions.Where(session => visibleGames.Contains(session.GameKey)).ToArray();", source);
        Assert.Contains("_visibleIds = _exportSessions.Where", source);
        Assert.Contains("_games = new string?[] { null }.Concat(_all", source);
        Assert.DoesNotContain("dd.MM.yy", source);
    }
}

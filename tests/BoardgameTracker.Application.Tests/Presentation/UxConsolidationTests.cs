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
        Assert.DoesNotContain(page.Descendants(), element => element.Name.LocalName == "Border");
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

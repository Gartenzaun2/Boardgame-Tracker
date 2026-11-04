using System.Reflection;
using System.Xml.Linq;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class AppPageHeaderTests
{
    private static Assembly Assembly => typeof(AppPageHeaderTests).Assembly;
    internal static XDocument ReadPage(string name)
    {
        string[] matches = Assembly.GetManifestResourceNames().Where(resource => resource.EndsWith("." + name + ".xaml", StringComparison.Ordinal)).ToArray();
        string resource = matches.FirstOrDefault(resource => resource.StartsWith("UxContract.", StringComparison.Ordinal)) ?? Assert.Single(matches);
        using StreamReader reader = new(Assembly.GetManifestResourceStream(resource)!);
        return XDocument.Parse(reader.ReadToEnd());
    }
    internal static string ReadHeaderText()
    {
        using StreamReader reader = new(Assembly.GetManifestResourceStream("HeaderContract.AppPageHeader.xaml")!);
        return reader.ReadToEnd();
    }
    private static XElement Header(XDocument page) => Assert.Single(page.Descendants(), element =>
        element.Name.LocalName == "AppPageHeader" && (string?)element.Attribute(XName.Get("Name", "http://schemas.microsoft.com/winfx/2009/xaml")) == "PageHeader");

    [Theory]
    [InlineData("GamesPage")]
    [InlineData("HistoryPage")]
    [InlineData("PlayerListPage")]
    [InlineData("AppSettingsPage")]
    public void MainSectionsUseSameHeaderAndKeepControlsBelowDivider(string page)
    {
        var document = ReadPage(page);
        var header = Header(document);
        Assert.Equal("MainSection", (string?)header.Attribute("Variant"));
        Assert.Null(header.Attribute("BadgeText"));
        Assert.Null(header.Attribute("BackClicked"));
        if (page == "PlayerListPage")
        {
            var segments = document.Descendants().Where(element => (string?)element.Attribute("Clicked") is "OnPlayersSegmentClicked" or "OnGroupsSegmentClicked").ToArray();
            Assert.Equal(2, segments.Length);
            foreach (var segment in segments)
            {
                Assert.DoesNotContain(header, segment.Ancestors());
                Assert.True(XNode.DocumentOrderComparer.Compare(header, segment) < 0);
            }
        }
    }

    [Theory]
    [InlineData("EditPlayerPage")]
    [InlineData("CreateGroupPage")]
    [InlineData("DuplicateGroupPage")]
    [InlineData("EditGroupPage")]
    [InlineData("MyGamesPage")]
    [InlineData("LicenseListPage")]
    [InlineData("LicenseDetailPage")]
    public void GeneralSubpagesUseReservedBackSlotWithoutGameBadge(string page)
    {
        var header = Header(ReadPage(page));
        Assert.Equal("Subpage", (string?)header.Attribute("Variant"));
        Assert.NotNull(header.Attribute("BackClicked"));
        Assert.Null(header.Attribute("BadgeText"));
        Assert.Null(header.Attribute("GameName"));
    }

    [Theory]
    [InlineData("ArkNovaScoringPage", "{Binding DateText}")]
    [InlineData("ArkNovaResultPage", "{Binding DurationText}")]
    [InlineData("ArkNovaTieResolutionPage", null)]
    public void RelatedSessionScreensShareTitleAndContextGeometry(string page, string? metadata)
    {
        var header = Header(ReadPage(page));
        Assert.Equal("Subpage", (string?)header.Attribute("Variant"));
        Assert.Equal("{Binding GroupName}", (string?)header.Attribute("Subtitle"));
        Assert.Equal(metadata, (string?)header.Attribute("Metadata"));
        // Pages keep their body padding. The header cancels it so its own anchors
        // are identical even where the body uses a narrower scoring layout.
        var parent = header.Ancestors().First(element => element.Attribute("Padding") is not null);
        double padding = double.Parse(parent.Attribute("Padding")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        double[] margin = header.Attribute("Margin")!.Value.Split(',').Select(value => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(0, padding + margin[0]);
        Assert.Equal(0, padding + margin[1]);
        Assert.Equal(0, padding + margin[2]);
        Assert.Null(header.Attribute("FontSize"));
        Assert.Null(header.Attribute("Padding"));
    }

    [Fact]
    public void SharedTemplateKeepsAnchorsAndAllowsRealContextHeight()
    {
        var template = XDocument.Parse(ReadHeaderText());
        var title = Assert.Single(template.Descendants(), element => (string?)element.Attribute("Text") == "{Binding Title, Source={x:Reference Header}}");
        Assert.Equal("1", (string?)title.Attribute("Grid.Column"));
        Assert.Equal("24", (string?)title.Attribute("FontSize"));
        Assert.Equal("Start", (string?)title.Attribute("VerticalOptions"));
        Assert.Equal("{Binding Title, Source={x:Reference Header}}", (string?)title.Attribute("SemanticProperties.Description"));
        var actionRow = Assert.Single(template.Descendants(), element => element.Name.LocalName == "HorizontalStackLayout");
        Assert.Equal("End", (string?)actionRow.Attribute("HorizontalOptions"));
        Assert.Equal("4", (string?)actionRow.Attribute("Spacing"));
        Assert.Equal("Auto", (string?)title.Parent!.Elements().First(element => element.Name.LocalName == "Grid.ColumnDefinitions").Elements().Last().Attribute("Width"));
        var capsule = Assert.Single(template.Descendants(), element => element.Name.LocalName == "Border");
        Assert.Equal("RoundRectangle 14", (string?)capsule.Attribute("StrokeShape"));
        Assert.Equal("{Binding GameName, Source={x:Reference Header}}", (string?)capsule.Attribute("SemanticProperties.Description"));
        Assert.Equal("28", (string?)capsule.Attribute("MinimumHeightRequest"));
        Assert.Null(capsule.Attribute("HeightRequest"));
        foreach (string text in new[] { "Subtitle", "Metadata" })
        {
            var label = Assert.Single(template.Descendants(), element => (string?)element.Attribute("Text") == "{Binding " + text + ", Source={x:Reference Header}}");
            Assert.Equal("WordWrap", (string?)label.Attribute("LineBreakMode"));
            Assert.Null(label.Attribute("HeightRequest"));
            Assert.Null(label.Attribute("MaxLines"));
        }
        Assert.Equal("BoxView", template.Root!.Elements().Single().Elements().Last().Name.LocalName);
        using StreamReader code = new(Assembly.GetManifestResourceStream("HeaderContract.AppPageHeader.xaml.cs")!);
        string source = code.ReadToEnd();
        Assert.Contains("HasBadge || !string.IsNullOrWhiteSpace(Subtitle) || HasMetadata", source);
        Assert.Contains("MainSection ? 0 : 48", source);
        Assert.DoesNotContain("ArkNova", source);
        Assert.DoesNotContain("GoToAsync", source);
        Assert.DoesNotContain("Shell", source);
    }

    [Theory]
    [InlineData("AppSettingsPage", 1)]
    [InlineData("CreateGroupPage", 2)]
    [InlineData("ArkNovaScoringPage", 3)]
    public void OneToThreeActionsUseOnePackedRightAlignedSlot(string page, int count)
    {
        var header = Header(ReadPage(page));
        var slot = Assert.Single(header.Elements(), element => element.Name.LocalName == "AppPageHeader.Actions");
        Assert.Equal(count, slot.Elements().Count());
        foreach (var action in slot.Elements())
        {
            Assert.Null(action.Attribute("Grid.Column"));
            Assert.Null(action.Attribute("Margin"));
        }
        if (page == "ArkNovaScoringPage")
        {
            Assert.Equal(2, slot.Elements().Count(element => element.Attribute("IsVisible") is not null));
            Assert.Equal("OnSettingsClicked", (string?)slot.Elements().Last().Attribute("Clicked"));
        }
    }

    [Fact]
    public void ZeroOneAndTwoContextLinesRemainOptionalAndPhotoReviewKeepsItsModal()
    {
        Assert.Null(Header(ReadPage("MyGamesPage")).Attribute("Subtitle"));
        Assert.NotNull(Header(ReadPage("ArkNovaPreparationPage")).Attribute("Subtitle"));
        Assert.NotNull(Header(ReadPage("ArkNovaResultPage")).Attribute("Metadata"));
        var scoring = ReadPage("ArkNovaScoringPage");
        var review = Assert.Single(scoring.Descendants(), element => (string?)element.Attribute("Title") == "{x:Static res:AppResources.PhotoReviewTitle}");
        Assert.NotNull(review.Attribute("BadgeText"));
        Assert.Equal("OnCancelPhotoClicked", (string?)review.Attribute("BackClicked"));
        Assert.Contains(review.Ancestors(), element => (string?)element.Attribute("IsVisible") == "{Binding IsPhotoReviewVisible}");
    }

    [Fact]
    public void VisibleVocabularyDistinguishesCreationFromDraftAddition()
    {
        Assert.Equal("Spieler anlegen", AppResources.AddPlayer);
        Assert.Equal("Spezialkarte anlegen", AppResources.NewSpecialCard);
        Assert.Equal("Neuer Spieler", AppResources.NewPlayerTitle);
        Assert.Equal("Hinzufügen", AppResources.AddToDraft);
        var overlayTitle = ReadPage("CreateGroupPage").Descendants().Single(element =>
            element.Name.LocalName == "Label" && (string?)element.Attribute("Text") == "{x:Static res:AppResources.AddPlayer}");
        Assert.Contains(overlayTitle.Ancestors(), element => (string?)element.Attribute("IsVisible") == "{Binding IsAddingNewPlayer}");
        Assert.Equal("{x:Static res:AppResources.Settings}", (string?)Header(ReadPage("ArkNovaSettingsPage")).Attribute("Title"));
    }
}

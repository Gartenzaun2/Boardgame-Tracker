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
        Assert.Null(header.Attribute("Margin"));
        Assert.DoesNotContain(header.Ancestors(), element => element.Attribute("Padding") is not null);
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
        Assert.Null(header.Attribute("Margin"));
        Assert.DoesNotContain(header.Ancestors(), element => element.Attribute("Padding") is not null);
        var body = Assert.Single(header.Ancestors().First(element => element.Name.LocalName == "Grid").Elements(),
            element => (string?)element.Attribute("Grid.Row") == "1" && element.Attribute("Padding") is not null);
        Assert.Equal(page == "ArkNovaScoringPage" ? "14,0,14,14" : "18,0,18,18", (string?)body.Attribute("Padding"));
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
        Assert.Equal("RoundRectangle 12", (string?)capsule.Attribute("StrokeShape"));
        Assert.Equal("{Binding GameName, Source={x:Reference Header}}", (string?)capsule.Attribute("SemanticProperties.Description"));
        Assert.Equal("24", (string?)capsule.Attribute("MinimumHeightRequest"));
        Assert.Equal("24", (string?)capsule.Attribute("HeightRequest"));
        Assert.Equal("24", (string?)capsule.Attribute("MaximumHeightRequest"));
        Assert.Equal("Bold", (string?)capsule.Elements().Single().Attribute("FontAttributes"));
        Assert.Equal("13", (string?)capsule.Elements().Single().Attribute("FontSize"));
        Assert.Null(capsule.Attribute("Grid.Column"));
        Assert.Equal("Auto,*", (string?)capsule.Parent!.Attribute("ColumnDefinitions"));
        Assert.Equal("6", (string?)capsule.Parent.Attribute("ColumnSpacing"));
        Assert.Null(capsule.Parent.Attribute("MinimumHeightRequest"));
        Assert.Equal("8,0", (string?)capsule.Attribute("Padding"));
        Assert.Equal("2", (string?)capsule.Attribute("StrokeThickness"));
        Assert.Equal("Transparent", (string?)capsule.Attribute("BackgroundColor"));
        Assert.Equal("Start", (string?)capsule.Attribute("HorizontalOptions"));
        Assert.Equal("Center", (string?)capsule.Attribute("VerticalOptions"));
        Assert.Null(capsule.Attribute("WidthRequest"));
        Assert.Null(capsule.Attribute("MinimumWidthRequest"));
        var badgeText = capsule.Elements().Single();
        Assert.Equal("Center", (string?)badgeText.Attribute("VerticalTextAlignment"));
        Assert.Equal("False", (string?)badgeText.Attribute("FontAutoScalingEnabled"));
        Assert.DoesNotContain(capsule.Descendants(), element => element.Name.LocalName == "Shadow");
        var subtitle = Assert.Single(template.Descendants(), element => (string?)element.Attribute("Text") == "{Binding Subtitle, Source={x:Reference Header}}");
        Assert.Equal("Center", (string?)subtitle.Attribute("VerticalOptions"));
        Assert.Null(subtitle.Attribute("FontAutoScalingEnabled"));
        var noBadge = Assert.Single(capsule.Parent.Descendants(), element => element.Name.LocalName == "DataTrigger");
        Assert.Equal("{Binding HasBadge, Source={x:Reference Header}}", (string?)noBadge.Attribute("Binding"));
        Assert.Equal("False", (string?)noBadge.Attribute("Value"));
        var spacing = Assert.Single(noBadge.Elements());
        Assert.Equal("ColumnSpacing", (string?)spacing.Attribute("Property"));
        Assert.Equal("0", (string?)spacing.Attribute("Value"));
        Assert.Same(title.Parent!.Parent, capsule.Parent.Parent);
        Assert.Equal("4", (string?)template.Root!.Elements().Single().Attribute("Spacing"));
        var metadata = Assert.Single(template.Descendants(), element => (string?)element.Attribute("Text") == "{Binding Metadata, Source={x:Reference Header}}");
        Assert.Null(metadata.Attribute("Grid.Column"));
        Assert.Null(metadata.Parent!.Attribute("ColumnDefinitions"));
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
            if (action.Name.LocalName == "ImageButton")
                Assert.Equal("{StaticResource HeaderIconButton}", (string?)action.Attribute("Style"));
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

using System.Reflection;
using BoardgameTracker.App.Presentation.Controls;
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
        Assert.Null(title.Attribute("Margin"));
        Assert.Null(title.Attribute("Grid.Column"));
        Assert.Equal("AppPageHeaderLayout", title.Parent!.Name.LocalName);
        Assert.Equal("24", (string?)title.Attribute("FontSize"));
        Assert.Equal("Center", (string?)title.Attribute("VerticalOptions"));
        Assert.Equal("Center", (string?)title.Attribute("VerticalTextAlignment"));
        Assert.Equal("{Binding Title, Source={x:Reference Header}}", (string?)title.Attribute("SemanticProperties.Description"));
        var actionRow = Assert.Single(template.Descendants(), element => element.Name.LocalName == "HorizontalStackLayout");
        Assert.Equal("End", (string?)actionRow.Attribute("HorizontalOptions"));
        Assert.Equal("0", (string?)actionRow.Attribute("Spacing"));
        Assert.Equal("Center", (string?)actionRow.Attribute("VerticalOptions"));
        Assert.Equal(6, title.Parent!.Elements().Count());
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
        Assert.Same(title.Parent, capsule.Parent.Parent);
        Assert.Null(template.Root!.Elements().Single().Attribute("Margin"));
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
        Assert.Contains("HasBadge || !string.IsNullOrWhiteSpace(Subtitle);", source);
        Assert.Contains("image.WidthRequest = 44", source);
        Assert.Contains("image.HeightRequest = 44", source);
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

    [Theory]
    [InlineData(false, 29)]
    [InlineData(true, 29)]
    [InlineData(false, 60)]
    [InlineData(true, 60)]
    public void MeasuredHeaderAlignsVisibleAnchorsCentersAndCompleteTouchAreas(bool subpage, double titleHeight)
    {
        var frame = AppPageHeaderGeometry.Create(360, titleHeight, 132, 44, 24, 18, 1, subpage);
        double Center(HeaderBox box) => box.Y + box.Height / 2;
        Assert.Equal(16, frame.Context.X);
        Assert.Equal(16, frame.Metadata.X);
        // icon_back.svg has its tip at x=4 and rightmost point at x=20,
        // within a 24-dp image inside ten dp of touch padding.
        Assert.Equal(16, frame.Back.X + 10 + 4);
        Assert.Equal(subpage ? 44 : 16, frame.Title.X);
        if (subpage) Assert.Equal(12, frame.Title.X - (frame.Back.X + 10 + 20));
        Assert.Equal(Center(frame.Title), Center(frame.Back));
        Assert.Equal(Center(frame.Title), Center(frame.Actions));
        Assert.Equal(44, frame.Back.Height);
        Assert.True(frame.Back.Y >= 0);
        Assert.True(frame.Back.Y + frame.Back.Height <= frame.Context.Y);
        Assert.True(frame.Actions.Y + frame.Actions.Height <= frame.Context.Y);
        Assert.True(frame.Back.X + frame.Back.Width <= frame.Actions.X);
        Assert.True(frame.Title.X + frame.Title.Width <= frame.Actions.X);
        Assert.Equal(4, frame.Context.Y - (frame.Title.Y + frame.Title.Height));
        Assert.Equal(4, frame.Metadata.Y - (frame.Context.Y + frame.Context.Height));
        Assert.Equal(4, frame.Divider.Y - (frame.Metadata.Y + frame.Metadata.Height));
    }

    [Fact]
    public void IconCountAndMissingContextOnlyChangeTheirOwnSpace()
    {
        var one = AppPageHeaderGeometry.Create(320, 29, 44, 44, 0, 0, 1, true);
        var three = AppPageHeaderGeometry.Create(320, 29, 132, 44, 24, 18, 1, true);
        Assert.Equal(one.Title.X, three.Title.X);
        Assert.Equal(one.Title.Y, three.Title.Y);
        Assert.Equal(one.Back, three.Back);
        Assert.Equal(one.Actions.X + 22, three.Actions.X + 2 * 44 + 22);
        Assert.Equal(44, (three.Actions.X + 44 + 22) - (three.Actions.X + 22));
        Assert.Equal(44, one.Divider.Y);
        Assert.Equal(45, one.Height);
        Assert.True(one.Back.Y + one.Back.Height <= one.Height);
        Assert.True(three.Height > one.Height);
        var metadataOnly = AppPageHeaderGeometry.Create(320, 29, 0, 0, 0, 18, 1, false);
        Assert.Equal(44, metadataOnly.Metadata.Y);
        Assert.Equal(66, metadataOnly.Divider.Y);
    }

    [Theory]
    [InlineData(240)]
    [InlineData(320)]
    [InlineData(768)]
    public void NarrowWidthsAndLongContextKeepTouchAreasOutOfContext(double width)
    {
        var frame = AppPageHeaderGeometry.Create(width, 60, 132, 44, 96, 36, 1, true);
        Assert.True(frame.Title.Width >= 0);
        Assert.Equal(width - 32, frame.Context.Width);
        Assert.True(frame.Actions.Y + frame.Actions.Height <= frame.Context.Y);
        Assert.True(frame.Back.Y + frame.Back.Height <= frame.Context.Y);
        Assert.Equal(frame.Context.Y + 96 + 4, frame.Metadata.Y);
    }

    [Fact]
    public void HistoricalOrderUsesCenteredWrappingPairsAndRetainsAccessibleText()
    {
        var page = ReadPage("ArkNovaResultPage");
        var flow = Assert.Single(page.Descendants(), element => element.Name.LocalName == "ArkNovaStartingOrderLayout");
        Assert.Equal("{Binding StartingOrderPlayers}", (string?)flow.Attribute("BindableLayout.ItemsSource"));
        var leading = Assert.Single(flow.Descendants(), element => (string?)element.Attribute("Text") == "{Binding LeadingText}");
        var name = Assert.Single(flow.Descendants(), element => (string?)element.Attribute("Text") == "{Binding Name}");
        Assert.Same(leading.Parent, name.Parent);
        Assert.Equal("ArkNovaStartingOrderSection", leading.Parent!.Name.LocalName);
        Assert.Equal("{Binding HasPrevious}", (string?)leading.Parent.Attribute("HasPrevious"));
        Assert.Equal("Center", (string?)leading.Attribute("VerticalOptions"));
        Assert.Equal("Center", (string?)name.Attribute("VerticalOptions"));
        Assert.Equal("WordWrap", (string?)name.Attribute("LineBreakMode"));
        Assert.Null(name.Attribute("MaxLines"));
        Assert.Null(leading.Parent.Attribute("ColumnDefinitions"));
        Assert.Null(leading.Parent.Attribute("WidthRequest"));
        Assert.Equal("{Binding StartingOrderText}", (string?)flow.Attribute("SemanticProperties.Description"));
    }

    [Theory]
    [InlineData(240)]
    [InlineData(320)]
    [InlineData(768)]
    public void DividerHasIndependentSixDpInsetsForBothVariants(double width)
    {
        foreach (bool subpage in new[] { false, true })
        {
            var frame = AppPageHeaderGeometry.Create(width, 29, 44, 44, 24, 18, 1, subpage);
            Assert.Equal(6, frame.Divider.X);
            Assert.Equal(width - 12, frame.Divider.Width);
            Assert.Equal(6, width - frame.Divider.X - frame.Divider.Width);
            Assert.Equal(1, frame.Divider.Height);
            Assert.Equal(16, frame.Context.X);
            Assert.Equal(16, frame.Metadata.X);
            Assert.Equal(subpage ? 44 : 16, frame.Title.X);
            Assert.Equal(94, frame.Divider.Y);
        }
    }
}

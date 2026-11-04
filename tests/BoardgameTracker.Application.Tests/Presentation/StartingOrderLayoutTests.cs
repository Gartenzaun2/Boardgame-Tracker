using System.Xml.Linq;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class StartingOrderLayoutTests
{
    private static XElement Flow => Assert.Single(AppPageHeaderTests.ReadPage("ArkNovaResultPage").Descendants(),
        element => (string?)element.Attribute("BindableLayout.ItemsSource") == "{Binding StartingOrderPlayers}");
    private static XElement Section => Assert.Single(Flow.Descendants(),
        element => element.Name.LocalName == "Grid");

    [Fact]
    public void NativeFlowWrapsWholeArrowNameSectionsAtTheAvailableWidth()
    {
        Assert.Equal("FlexLayout", Flow.Name.LocalName);
        Assert.Equal("Row", (string?)Flow.Attribute("Direction"));
        Assert.Equal("Wrap", (string?)Flow.Attribute("Wrap"));
        Assert.Equal("Start", (string?)Flow.Attribute("JustifyContent"));
        Assert.Equal("Start", (string?)Flow.Attribute("AlignContent"));
        Assert.DoesNotContain(Flow.Descendants(), element => element.Name.LocalName == "FlexLayout");
        Assert.Equal("Auto,*", (string?)Section.Attribute("ColumnDefinitions"));
        Assert.Equal("0", (string?)Section.Attribute("FlexLayout.Grow"));
        Assert.Equal("1", (string?)Section.Attribute("FlexLayout.Shrink"));
        Assert.Equal("Start", (string?)Section.Attribute("HorizontalOptions"));
    }

    [Fact]
    public void IntrinsicSectionsHaveExactlyOneFiveDpGapPerTransition()
    {
        Assert.Equal("0,0,5,4", (string?)Section.Attribute("Margin"));
        var labels = Section.Elements().Where(element => element.Name.LocalName == "Label").ToArray();
        Assert.Equal(2, labels.Length);
        Assert.Equal("{Binding LeadingText}", (string?)labels[0].Attribute("Text"));
        Assert.Null(labels[0].Attribute("Margin"));
        Assert.Equal("{Binding Name}", (string?)labels[1].Attribute("Text"));
        Assert.Null(labels[1].Attribute("Margin"));
        Assert.Equal("5", (string?)Section.Attribute("ColumnSpacing"));
        Assert.Equal("1", (string?)labels[1].Attribute("Grid.Column"));
        foreach (var element in Flow.DescendantsAndSelf())
        {
            if (element.Name.LocalName != "Image") Assert.Null(element.Attribute("WidthRequest"));
            Assert.Null(element.Attribute("Padding"));
            Assert.Null(element.Attribute("FlexLayout.Basis"));
        }
    }

    [Fact]
    public void NamesCanShrinkAndWrapWhileTheirLeadingElementRemainsVisible()
    {
        var labels = Section.Elements().Where(element => element.Name.LocalName == "Label").ToArray();
        Assert.Equal("Auto,*", (string?)Section.Attribute("ColumnDefinitions"));
        Assert.Equal("WordWrap", (string?)labels[1].Attribute("LineBreakMode"));
        Assert.Null(labels[1].Attribute("MaxLines"));
        Assert.Equal("Center", (string?)Flow.Attribute("AlignItems"));
        Assert.All(labels, label => Assert.Equal("Center", (string?)label.Attribute("VerticalOptions")));
        Assert.All(labels, label =>
        {
            Assert.Equal("13", (string?)label.Attribute("FontSize"));
            Assert.Equal("Center", (string?)label.Attribute("VerticalTextAlignment"));
            Assert.Null(label.Attribute("FontAutoScalingEnabled"));
        });
    }

    [Fact]
    public void EmptyAndSinglePlayerSequencesNeedNoSeparateCaptionColumn()
    {
        Assert.Single(Flow.Elements());
        Assert.Equal("{Binding StartingOrderText}", (string?)Flow.Attribute("SemanticProperties.Description"));
        var first = new BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result.ArkNovaStartingOrderPlayer("Claudi", false);
        Assert.Equal("Start:", first.LeadingText);
        Assert.Equal("Claudi", first.Name);
        Assert.Empty(new BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result.ArkNovaStartingOrderPlayer("Franz", true).LeadingText);
    }
    [Fact]
    public void ArrowIsASmallCenteredThemedDecorativeImageOnlyAfterTheFirstPlayer()
    {
        var image = Assert.Single(Section.Elements(), element => element.Name.LocalName == "Image");
        Assert.Equal("icon_starting_order_arrow.png", (string?)image.Attribute("Source"));
        Assert.Equal("14", (string?)image.Attribute("WidthRequest"));
        Assert.Equal("12", (string?)image.Attribute("HeightRequest"));
        Assert.Equal("Center", (string?)image.Attribute("VerticalOptions"));
        Assert.Equal("{Binding HasPrevious}", (string?)image.Attribute("IsVisible"));
        Assert.Equal("{DynamicResource TextPrimary}", (string?)image.Attribute(XName.Get("IconTint.Color", "clr-namespace:BoardgameTracker.App.Presentation")));
        Assert.Equal("False", (string?)image.Attribute("AutomationProperties.IsInAccessibleTree"));
        Assert.Null(image.Attribute("SemanticProperties.Description"));
        var trigger = Assert.Single(Section.Descendants(), element => element.Name.LocalName == "DataTrigger");
        Assert.Equal("{Binding HasPrevious}", (string?)trigger.Attribute("Binding"));
        Assert.Equal("True", (string?)trigger.Attribute("Value"));
        var setter = Assert.Single(trigger.Elements());
        Assert.Equal("IsVisible", (string?)setter.Attribute("Property"));
        Assert.Equal("False", (string?)setter.Attribute("Value"));
        Assert.DoesNotContain(Section.Descendants(), element => (string?)element.Attribute("Text") == "→");
    }
}

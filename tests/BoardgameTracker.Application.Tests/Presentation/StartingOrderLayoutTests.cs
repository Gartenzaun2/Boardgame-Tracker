using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class StartingOrderLayoutTests
{
    [Fact]
    public void WideLineKeepsFiveDpGapsAndOneVerticalCenter()
    {
        var first = ArkNovaStartingOrderGeometry.Section(500, new(30, 18), new(42, 18), false);
        var second = ArkNovaStartingOrderGeometry.Section(500, new(12, 14), new(35, 18), false);
        var third = ArkNovaStartingOrderGeometry.Section(500, new(12, 14), new(48, 18), false);
        var result = ArkNovaStartingOrderGeometry.Rows(500, [first.Size, second.Size, third.Size]);
        Assert.Equal(5, first.Name.X - first.Leading.Width);
        Assert.Equal(5, second.Name.X - second.Leading.Width);
        Assert.Equal(5, result.Boxes[1].X - (result.Boxes[0].X + result.Boxes[0].Width));
        Assert.Equal(5, result.Boxes[2].X - (result.Boxes[1].X + result.Boxes[1].Width));
        Assert.Equal(first.Leading.Y + first.Leading.Height / 2, first.Name.Y + first.Name.Height / 2);
        Assert.Equal(second.Leading.Y + second.Leading.Height / 2, second.Name.Y + second.Name.Height / 2);
        Assert.All(result.Boxes, box => Assert.Equal(0, box.Y));
        Assert.Equal(18, result.Height);
    }

    [Fact]
    public void NarrowExampleMovesWholeArrowNameSectionsWithoutContinuationIndent()
    {
        // Measured sections for Start: Claudi, → Franz, → Gernot, → Maximilian, → Anna.
        var result = ArkNovaStartingOrderGeometry.Rows(150,
            [new(78, 18), new(48, 18), new(60, 18), new(85, 18), new(45, 18)]);
        Assert.Equal(new[] { 0d, 83, 0, 65, 0 }, result.Boxes.Select(box => box.X));
        Assert.Equal(new[] { 0d, 0, 22, 22, 44 }, result.Boxes.Select(box => box.Y));
        Assert.All(result.Boxes, box => Assert.True(box.X + box.Width <= 150));
        Assert.Equal(62, result.Height);
    }

    [Theory]
    [InlineData(180, 1)]
    [InlineData(240, 1.5)]
    [InlineData(320, 2)]
    public void OversizedNameWrapsInsideAvailableSpaceWithItsArrowCentered(double width, double fontScale)
    {
        var leading = new StartingOrderSize(12 * fontScale, 18 * fontScale);
        double available = ArkNovaStartingOrderGeometry.NameWidth(width, leading, false);
        Assert.Equal(width - leading.Width - 5, available);
        var section = ArkNovaStartingOrderGeometry.Section(width, leading, new(available, 54 * fontScale), false);
        Assert.Equal(width, section.Size.Width);
        Assert.Equal(width, section.Name.X + section.Name.Width);
        Assert.Equal(section.Leading.Y + section.Leading.Height / 2, section.Name.Y + section.Name.Height / 2);
        var rows = ArkNovaStartingOrderGeometry.Rows(width, [section.Size, new(60, 18)]);
        Assert.Equal(0, rows.Boxes[1].X);
        Assert.True(rows.Boxes[1].Y >= section.Size.Height + 4);
    }

    [Fact]
    public void CaptionCanSeparateOnlyWhenFirstNameOtherwiseWouldWrapUnnecessarily()
    {
        var leading = new StartingOrderSize(30, 18);
        var name = new StartingOrderSize(180, 18);
        Assert.True(ArkNovaStartingOrderGeometry.CaptionNeedsOwnLine(200, leading, name, false));
        Assert.False(ArkNovaStartingOrderGeometry.CaptionNeedsOwnLine(200, leading, name, true));
        Assert.False(ArkNovaStartingOrderGeometry.CaptionNeedsOwnLine(200, leading, new(210, 18), false));
        var section = ArkNovaStartingOrderGeometry.Section(200, leading, name, true);
        Assert.Equal(0, section.Leading.X);
        Assert.Equal(0, section.Name.X);
        Assert.Equal(22, section.Name.Y);
        Assert.Equal(180, section.Name.Width);
        Assert.Equal(200, section.Size.Width);
    }

    [Fact]
    public void EmptyAndSinglePlayerSequencesDoNotReservePhantomColumnsOrRows()
    {
        Assert.Equal(0, ArkNovaStartingOrderGeometry.Rows(200, []).Height);
        var single = ArkNovaStartingOrderGeometry.Rows(200, [new(85, 18)]);
        var only = Assert.Single(single.Boxes);
        Assert.Equal(0, only.X);
        Assert.Equal(0, only.Y);
        Assert.Equal(85, only.Width);
        Assert.Equal(18, single.Height);
        var player = new ArkNovaStartingOrderPlayer("Claudi", false);
        Assert.Equal("Start:", player.LeadingText);
        Assert.Equal("→", new ArkNovaStartingOrderPlayer("Franz", true).LeadingText);
    }
}

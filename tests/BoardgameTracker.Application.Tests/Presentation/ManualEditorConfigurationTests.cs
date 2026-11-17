using System.Xml.Linq;
using BoardgameTracker.App.Games.ManualGames.Features.Settings;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Games.ManualGames;
using BoardgameTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class ManualEditorConfigurationTests {
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("AllowNegativePoints", "CapturePoints")]
    [InlineData("CapturePlacements", "CaptureWinner")]
    [InlineData("AllowTies", "CaptureWinner")]
    [InlineData("ResultFromPoints", "CanDeriveResult")]
    public void DependentOptions_KeepFixedRowsAndDisableOnlyTheirControl(string option, string prerequisite) {
        XDocument page = AppPageHeaderTests.ReadPage("ManualGameEditorPage");
        XElement label = Assert.Single(page.Descendants(), element => (string?)element.Attribute("Text") == "{x:Static res:AppResources." + option + "}");
        XElement row = label.Parent!;
        Assert.Equal("{StaticResource SettingsRow}", (string?)row.Attribute("Style"));
        Assert.Equal("16,0,0,0", (string?)row.Attribute("Margin"));
        Assert.All(row.AncestorsAndSelf(), ancestor => Assert.Null(ancestor.Attribute("IsVisible")));
        Assert.Null(row.Attribute("IsEnabled"));
        Assert.DoesNotContain(row.Descendants(), element => element.Name.LocalName.EndsWith("Trigger", StringComparison.Ordinal));
        XElement control = Assert.Single(row.Descendants(), element => element.Name.LocalName == (option == "ResultFromPoints" ? "Button" : "Switch"));
        Assert.Equal("{Binding " + prerequisite + "}", (string?)control.Attribute("IsEnabled"));
        Assert.NotNull(control.Attribute("SemanticProperties.Hint"));
        Assert.Equal("{StaticResource SettingsItemLabel}", (string?)label.Attribute("Style"));
        Assert.Null(label.Attribute("IsEnabled"));
        Assert.Null(row.Attribute("HeightRequest"));
        Assert.Null(label.Attribute("MaxLines"));
    }

    [Fact]
    public void ConfigurationOrderAndAccessibleInfo_RemainIndependentOfPrerequisites() {
        XDocument page = AppPageHeaderTests.ReadPage("ManualGameEditorPage");
        string[] options = ["CapturePoints", "AllowNegativePoints", "CaptureWinner", "CapturePlacements", "AllowTies", "ResultFromPoints", "CapturePlayerOrder", "CapturePlayerColors"];
        string[] expected = options.Select(option => "{x:Static res:AppResources." + option + "}").ToArray();
        XElement[] labels = page.Descendants().Where(element => element.Name.LocalName == "Label" && expected.Contains((string?)element.Attribute("Text"))).ToArray();
        Assert.Equal(expected, labels.Select(label => (string?)label.Attribute("Text")));
        foreach (XElement label in labels.TakeLast(2)) {
            Assert.Null(label.Parent!.Attribute("Margin"));
            Assert.Null(label.Parent.Attribute("IsVisible"));
            Assert.Null(label.Parent.Attribute("IsEnabled"));
        }
        XElement info = Assert.Single(page.Descendants(), element => (string?)element.Attribute("Clicked") == "OnResultModeInfoClicked");
        Assert.Equal("{StaticResource IconButton}", (string?)info.Attribute("Style"));
        Assert.NotNull(info.Attribute("SemanticProperties.Description"));
        Assert.All(info.AncestorsAndSelf(), element => Assert.DoesNotContain("CanDeriveResult", (string?)element.Attribute("IsEnabled") ?? string.Empty));
        Assert.All(page.Descendants().Where(element => element.Name.LocalName == "Border"), element => Assert.Equal("{StaticResource SettingsLookupFrame}", (string?)element.Attribute("Style")));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task Prerequisites_PreserveDraftValuesAndGuardTheRealLookupHandler(bool editing, bool points, bool winner) {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        TestGameEnabledStore store = new();
        ManualGameUseCase useCase = new(db, store);
        string? key = null;
        if (editing) {
            Guid id = await useCase.SaveAsync(new(null, "Existing", "E", 1, 4, new(false, false, false, false, false, false, null, false, false)), Token);
            await using BoardgameTrackerDbContext seed = db.CreateDbContext();
            key = (await seed.Games.SingleAsync(game => game.Id == id, Token)).Key;
        }
        ManualGameEditorViewModel editor = new(useCase, new(db, store), new(db, store), new EditorNavigationFake(), NullLogger<ManualGameEditorViewModel>.Instance);
        await editor.LoadAsync(key, Token);
        Assert.True(editor.CanEdit);
        editor.CapturePoints = true;
        editor.CaptureWinner = true;
        editor.AllowNegativePoints = true;
        editor.CapturePlacements = true;
        editor.AllowTies = true;
        editor.ResultMode = 2;
        List<string?> changes = [];
        editor.PropertyChanged += (_, args) => changes.Add(args.PropertyName);
        editor.CapturePoints = points;
        editor.CaptureWinner = winner;
        Assert.Equal(points && winner, editor.CanDeriveResult);
        Assert.True(editor.AllowNegativePoints);
        Assert.True(editor.CapturePlacements);
        Assert.True(editor.AllowTies);
        Assert.Equal(2, editor.ResultMode);
        if (!points || !winner)
            Assert.Contains(nameof(editor.CanDeriveResult), changes);

        EventBoundaryTestHost page = EventBoundaryTests.Create("ManualGameEditorPage.xaml.cs", manualEditor: editor);
        await page.InvokeAsync("OnResultModeClicked");
        Assert.Equal(points && winner ? 1 : 0, page.ResultChoiceCalls);
        Assert.Equal(points && winner ? 1 : 2, editor.ResultMode);
        Assert.False(editor.IsModalDialogOpen);
        if (points && winner)
            Assert.True(page.ResultChoiceWasModal);

        editor.CapturePoints = false;
        editor.CaptureWinner = false;
        await page.InvokeAsync("OnResultModeInfoClicked");
        Assert.Equal(AppResources.ResultFromPointsRequirement, Assert.Single(page.Messages));
        Assert.False(editor.IsModalDialogOpen);
        editor.CapturePoints = true;
        editor.CaptureWinner = true;
        Assert.True(editor.CanDeriveResult);
        Assert.True(editor.AllowNegativePoints && editor.CapturePlacements && editor.AllowTies);
        await page.InvokeAsync("OnResultModeClicked");
        Assert.Equal(points && winner ? 2 : 1, page.ResultChoiceCalls);
        Assert.Equal(1, editor.ResultMode);
        Assert.True(page.ResultChoiceWasModal);
    }
}

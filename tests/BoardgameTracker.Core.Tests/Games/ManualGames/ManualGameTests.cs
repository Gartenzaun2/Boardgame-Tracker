using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ManualGames;

namespace BoardgameTracker.Core.Tests.Games.ManualGames;

public sealed class ManualGameTests
{
    private static Game Game(int min = 1, int max = 12) => new(ManualGame.CreateKey(), " Name ", " Tag ", min, max);

    [Fact]
    public void FullConfiguration_AndMetadataUpdateKeepIdentityAndKey()
    {
        Game game = Game();
        Guid id = game.Id;
        string key = game.Key;
        ManualGame manual = new(game, new(true, true, true, true, true, true, PointsDirection.Lowest, true, true));
        manual.UpdateMetadata(game, " Changed ", " C ", 2, 12);
        Assert.Equal(id, manual.GameId);
        Assert.Equal(id, game.Id);
        Assert.Equal(key, game.Key);
        Assert.True(ManualGame.MatchesKey(key));
        Assert.NotEqual(key, ManualGame.CreateKey());
        Assert.Equal("Changed", game.Name);
        Assert.Equal("C", game.BadgeText);
        Assert.Equal(2, game.MinPlayers);
        Assert.True(manual.AllowNegativePoints && manual.CapturePlacements && manual.AllowTies);
        Assert.True(manual.ResultFromPoints && manual.CapturePlayerOrder && manual.CapturePlayerColors);
        Assert.Equal(PointsDirection.Lowest, manual.PointsDirection);
        Assert.Throws<InvalidOperationException>(() => manual.UpdateMetadata(game, "Other", "O", 1, 11));
        Assert.Equal(12, game.MaxPlayers);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Dependencies_AreNormalizedWithoutCouplingOrderOrColors(bool points, bool winner)
    {
        ManualGame manual = new(Game(), new(points, true, winner, true, true, true, PointsDirection.Highest, true, false));
        Assert.Equal(points, manual.AllowNegativePoints);
        Assert.Equal(winner, manual.CapturePlacements);
        Assert.Equal(winner, manual.AllowTies);
        Assert.Equal(points && winner, manual.ResultFromPoints);
        Assert.Equal(points && winner ? PointsDirection.Highest : (PointsDirection?)null, manual.PointsDirection);
        Assert.True(manual.CapturePlayerOrder);
        Assert.False(manual.CapturePlayerColors);
        manual.SetConfiguration(new(CapturePlayerColors: true));
        Assert.False(manual.CapturePoints || manual.CaptureWinner || manual.CapturePlacements || manual.ResultFromPoints);
        Assert.Null(manual.PointsDirection);
        Assert.True(manual.CapturePlayerColors);
    }

    [Fact]
    public void InvalidConfiguration_DoesNotChangePreviousValues()
    {
        ManualGame manual = new(Game(), new(CapturePlayerColors: true));
        Assert.Throws<ArgumentException>(() => manual.SetConfiguration(new(true, false, true, ResultFromPoints: true)));
        Assert.Throws<ArgumentException>(() => manual.SetConfiguration(new(true, false, true, ResultFromPoints: true, PointsDirection: (PointsDirection)99)));
        Assert.False(manual.CapturePoints);
        Assert.True(manual.CapturePlayerColors);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ManualGame(Game(max: 13), new()));
        Assert.Throws<ArgumentException>(() => new ManualGame(new("integrated", "Name", "N", 1, 4), new()));
    }

    [Theory]
    [InlineData("manual:")]
    [InlineData("manual:00000000-0000-0000-0000-000000000000")]
    [InlineData("manual:invalid")]
    [InlineData("ark-nova")]
    public void KeyNamespace_RejectsInvalidKeys(string key) => Assert.False(ManualGame.MatchesKey(key));
}

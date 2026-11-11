using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ManualGames;

namespace BoardgameTracker.Core.Tests.Games.ManualGames;

public sealed class ManualGameSessionTests
{
    [Fact]
    public void Snapshot_IsIndependentOfLaterConfigurationAndContainsOnlyScoringRules()
    {
        Game game = new(ManualGame.CreateKey(), "Manual", "M", 1, 12);
        ManualGame config = new(game, new(true, true, true, true, true, true, PointsDirection.Lowest, true, true));
        Guid id = Guid.NewGuid();
        ManualGameSession session = new(id, config);
        config.SetConfiguration(new());
        Assert.Equal(id, session.GameSessionId);
        Assert.Equal(new ScoringRulesSnapshot(true, true, true, true, true, true, PointsDirection.Lowest), session.ScoringRulesSnapshot);
        Assert.DoesNotContain(typeof(ScoringRulesSnapshot).GetProperties(), property => property.Name.Contains("Color") || property.Name.Contains("Order"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ManualGameSession(Guid.Empty, config));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(100)]
    public void HistoricalPoints_AreNullableIntegersWithoutCurrentConfigurationRestrictions(int? points)
    {
        ManualGameSessionPlayer player = new(Guid.NewGuid(), Guid.NewGuid(), points);
        Assert.Equal(points, player.Points);
    }
}

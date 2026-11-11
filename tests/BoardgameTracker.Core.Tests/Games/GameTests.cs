using BoardgameTracker.Core.Games;

namespace BoardgameTracker.Core.Tests.Games;

public sealed class GameTests
{
    [Fact]
    public void Metadata_IsTrimmedAndUpdatesPreserveIdentityAndKey()
    {
        string key = "manual:" + Guid.NewGuid();
        Game game = new(" " + key + " ", " Name ", " Badge ", 2, 4);
        Guid id = game.Id;
        Assert.Equal(key, game.Key);
        Assert.Equal("Name", game.Name);
        Assert.Equal("Badge", game.BadgeText);
        game.SetMetadata("Changed", "C", 1, 8);
        Assert.Equal(id, game.Id);
        Assert.Equal(key, game.Key);
        Assert.Equal("Changed", game.Name);
        Assert.True(game.SupportsPlayerCount(1));
        Assert.True(game.SupportsPlayerCount(8));
        Assert.False(game.SupportsPlayerCount(0));
        Assert.False(game.SupportsPlayerCount(9));
    }

    [Theory]
    [InlineData("", "Name", "Badge", 1, 4)]
    [InlineData(" ", "Name", "Badge", 1, 4)]
    [InlineData("key", " ", "Badge", 1, 4)]
    [InlineData("key", "Name", " ", 1, 4)]
    [InlineData("key", "Name", "Badge", 0, 4)]
    [InlineData("key", "Name", "Badge", 3, 2)]
    public void Construction_RejectsInvalidMetadata(string key, string name, string badge, int min, int max) =>
        Assert.ThrowsAny<ArgumentException>(() => new Game(key, name, badge, min, max));

    [Fact]
    public void LengthLimits_AndFailedUpdatesPreservePreviousValues()
    {
        Assert.Throws<ArgumentException>(() => new Game(new string('k', 101), "Name", "B", 1, 4));
        Game game = new(new string('k', 100), new string('n', 100), "B", 1, 4);
        Assert.Throws<ArgumentException>(() => game.SetMetadata(new string('n', 101), "B", 1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => game.SetMetadata("Changed", "C", 4, 2));
        Assert.Equal(new string('n', 100), game.Name);
        Assert.Equal("B", game.BadgeText);
        Assert.Equal(1, game.MinPlayers);
        Assert.Equal(4, game.MaxPlayers);
    }
}

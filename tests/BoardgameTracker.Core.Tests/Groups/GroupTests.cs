using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Core.Tests.Groups;

public sealed class GroupTests
{
    [Fact]
    public void Constructor_CreatesPlayersInGivenOrder()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        Group group = new("Freitagabend", false, [first, second]);

        Assert.Collection(
            group.Players.OrderBy(player => player.DefaultOrder),
            player =>
            {
                Assert.Equal(first, player.PlayerId);
                Assert.Equal(1, player.DefaultOrder);
            },
            player =>
            {
                Assert.Equal(second, player.PlayerId);
                Assert.Equal(2, player.DefaultOrder);
            });
    }

    [Fact]
    public void Constructor_RejectsDuplicatePlayers()
    {
        Guid playerId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            new Group("Freitagabend", false, [playerId, playerId]));
    }

    [Fact]
    public void SetDefaultOrder_RequiresExactlyTheExistingPlayers()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Freitagabend", false, [first, second]);

        group.SetDefaultOrder([second, first]);

        Assert.Equal(1, group.Players.Single(x => x.PlayerId == second).DefaultOrder);
        Assert.Equal(2, group.Players.Single(x => x.PlayerId == first).DefaultOrder);
    }

    [Fact]
    public void Constructor_RejectsEmptyPlayership()
    {
        Assert.Throws<ArgumentException>(() =>
            new Group("Freitagabend", false, []));
    }

    [Fact]
    public void SetDefaultOrder_RejectsMissingOrUnknownPlayers()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Freitagabend", false, [first, second]);

        Assert.Throws<ArgumentException>(() =>
            group.SetDefaultOrder([first, Guid.NewGuid()]));
    }
}

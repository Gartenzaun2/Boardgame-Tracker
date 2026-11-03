using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Core.Tests.Groups;

public sealed class GroupTests
{
    [Fact]
    public void Group_RetainsIdentityAndMembershipWhenRenamedAndArchived()
    {
        Guid playerId = Guid.NewGuid();
        Group group = new("Original", [playerId]);
        Guid id = group.Id;
        Assert.False(group.IsArchived);

        group.SetName(" Renamed ");
        group.SetArchived(true);

        Assert.Equal(id, group.Id);
        Assert.Equal("Renamed", group.Name);
        Assert.True(group.IsArchived);
        Assert.Equal(playerId, Assert.Single(group.Players).PlayerId);
    }

    [Fact]
    public void Constructor_CreatesPlayersInGivenOrder()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        Group group = new("Freitagabend", [first, second]);

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
            new Group("Freitagabend", [playerId, playerId]));
    }

    [Fact]
    public void SetDefaultOrder_RequiresExactlyTheExistingPlayers()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Freitagabend", [first, second]);

        group.SetDefaultOrder([second, first]);

        Assert.Equal(1, group.Players.Single(x => x.PlayerId == second).DefaultOrder);
        Assert.Equal(2, group.Players.Single(x => x.PlayerId == first).DefaultOrder);
    }

    [Fact]
    public void Constructor_RejectsEmptyPlayership()
    {
        Assert.Throws<ArgumentException>(() =>
            new Group("Freitagabend", []));
    }

    [Fact]
    public void SetDefaultOrder_RejectsMissingOrUnknownPlayers()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Freitagabend", [first, second]);

        Assert.Throws<ArgumentException>(() =>
            group.SetDefaultOrder([first, Guid.NewGuid()]));
    }
}

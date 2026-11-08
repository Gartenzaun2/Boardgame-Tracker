using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Core.Tests.Groups;

public sealed class GroupGameTests
{
    [Fact]
    public void Create_PreservesBothIdentitiesAtLimit()
    {
        Group group = new("Group", Enumerable.Range(1, 4).Select(_ => Guid.NewGuid()).ToArray());
        Game game = new("test");
        GroupGame assignment = GroupGame.Create(group, game, new(game.Key, 4));
        Assert.Equal(group.Id, assignment.GroupId);
        Assert.Equal(game.Id, assignment.GameId);
    }

    [Fact]
    public void Create_RejectsOversizedGroupWithErrorDetails()
    {
        Group group = new("Group", Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToArray());
        Game game = new("test");
        GroupPlayerLimitExceededException exception = Assert.Throws<GroupPlayerLimitExceededException>(() =>
            GroupGame.Create(group, game, new(game.Key, 4)));
        Assert.Equal(game.Key, exception.GameKey);
        Assert.Equal(4, exception.MaximumPlayers);
    }

    [Fact]
    public void Create_RejectsLimitForAnotherGame()
    {
        Group group = new("Group", [Guid.NewGuid()]);
        Assert.Throws<ArgumentException>(() => GroupGame.Create(group, new("test"), new("other", 4)));
    }

    [Fact]
    public void Create_RejectsNullArguments()
    {
        Group group = new("Group", [Guid.NewGuid()]);
        Game game = new("test");
        GamePlayerLimit limit = new(game.Key, 4);
        Assert.Throws<ArgumentNullException>(() => GroupGame.Create(null!, game, limit));
        Assert.Throws<ArgumentNullException>(() => GroupGame.Create(group, null!, limit));
        Assert.Throws<ArgumentNullException>(() => GroupGame.Create(group, game, null!));
    }

    [Fact]
    public void Create_RejectsEmptyMembership()
    {
        // Represents an entity materialized without its membership navigation.
        Group group = (Group)Activator.CreateInstance(typeof(Group), nonPublic: true)!;
        Assert.Throws<ArgumentException>(() => GroupGame.Create(group, new("test"), new("test", 4)));
    }

    [Fact]
    public void Construction_RequiresFactory()
    {
        Assert.Empty(typeof(GroupGame).GetConstructors());
    }
}

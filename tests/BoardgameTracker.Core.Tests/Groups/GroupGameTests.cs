using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Core.Tests.Groups;

public sealed class GroupGameTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void Create_PreservesBothIdentitiesAtEitherLimit(int count)
    {
        Group group = new("Group", Enumerable.Range(1, count).Select(_ => Guid.NewGuid()).ToArray());
        Game game = new("test", "Test", "T", 2, 4);
        GroupGame assignment = GroupGame.Create(group, game);
        Assert.Equal(group.Id, assignment.GroupId);
        Assert.Equal(game.Id, assignment.GameId);
    }

    [Fact]
    public void Create_RejectsOversizedGroupWithErrorDetails()
    {
        Group group = new("Group", Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToArray());
        Game game = new("test", "Test", "T", 2, 4);
        GroupPlayerLimitExceededException exception = Assert.Throws<GroupPlayerLimitExceededException>(() => GroupGame.Create(group, game));
        Assert.Equal(game.Key, exception.GameKey);
        Assert.Equal(4, exception.MaximumPlayers);
    }

    [Fact]
    public void Create_RejectsUndersizedGroupWithErrorDetails()
    {
        Group group = new("Group", [Guid.NewGuid()]);
        Game game = new("test", "Test", "T", 2, 4);
        GroupPlayerMinimumNotMetException exception = Assert.Throws<GroupPlayerMinimumNotMetException>(() => GroupGame.Create(group, game));
        Assert.Equal(game.Key, exception.GameKey);
        Assert.Equal(2, exception.MinimumPlayers);
    }

    [Fact]
    public void Create_RejectsNullArguments()
    {
        Group group = new("Group", [Guid.NewGuid()]);
        Game game = new("test", "Test", "T", 1, 4);
        Assert.Throws<ArgumentNullException>(() => GroupGame.Create(null!, game));
        Assert.Throws<ArgumentNullException>(() => GroupGame.Create(group, null!));
    }

    [Fact]
    public void Create_RejectsEmptyMembership()
    {
        // Represents an entity materialized without its membership navigation.
        Group group = (Group)Activator.CreateInstance(typeof(Group), nonPublic: true)!;
        Assert.Throws<ArgumentException>(() => GroupGame.Create(group, new("test", "Test", "T", 1, 4)));
    }

    [Fact]
    public void Construction_RequiresFactory() => Assert.Empty(typeof(GroupGame).GetConstructors());
}

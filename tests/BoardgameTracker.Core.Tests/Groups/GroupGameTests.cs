using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Core.Tests.Groups;

public sealed class GroupGameTests
{
    [Fact]
    public void Constructor_PreservesBothIdentities()
    {
        Guid groupId = Guid.NewGuid();
        Guid gameId = Guid.NewGuid();
        GroupGame assignment = new(groupId, gameId);
        Assert.Equal(groupId, assignment.GroupId);
        Assert.Equal(gameId, assignment.GameId);
    }

    [Fact]
    public void Constructor_RejectsEmptyIdentities()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GroupGame(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GroupGame(Guid.NewGuid(), Guid.Empty));
    }
}

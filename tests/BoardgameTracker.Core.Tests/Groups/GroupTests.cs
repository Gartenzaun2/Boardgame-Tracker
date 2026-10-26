using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Core.Tests.Groups;

public sealed class GroupTests
{
    [Fact]
    public void Constructor_CreatesMembersInGivenOrder()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        Group group = new("Freitagabend", false, [first, second]);

        Assert.Collection(
            group.Members.OrderBy(member => member.DefaultOrder),
            member =>
            {
                Assert.Equal(first, member.PersonId);
                Assert.Equal(1, member.DefaultOrder);
            },
            member =>
            {
                Assert.Equal(second, member.PersonId);
                Assert.Equal(2, member.DefaultOrder);
            });
    }

    [Fact]
    public void Constructor_RejectsDuplicateMembers()
    {
        Guid personId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            new Group("Freitagabend", false, [personId, personId]));
    }

    [Fact]
    public void SetDefaultOrder_RequiresExactlyTheExistingMembers()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Freitagabend", false, [first, second]);

        group.SetDefaultOrder([second, first]);

        Assert.Equal(1, group.Members.Single(x => x.PersonId == second).DefaultOrder);
        Assert.Equal(2, group.Members.Single(x => x.PersonId == first).DefaultOrder);
    }

    [Fact]
    public void Constructor_RejectsEmptyMembership()
    {
        Assert.Throws<ArgumentException>(() =>
            new Group("Freitagabend", false, []));
    }

    [Fact]
    public void SetDefaultOrder_RejectsMissingOrUnknownMembers()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Freitagabend", false, [first, second]);

        Assert.Throws<ArgumentException>(() =>
            group.SetDefaultOrder([first, Guid.NewGuid()]));
    }
}

using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Application.Tests.Groups.EditGroup;

public sealed class EditGroupUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ChangesNameOrderAndArchiveStateWithoutChangingPlayership()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Alt", false, [first, second]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess, new([]));

        await useCase.ExecuteAsync(new EditGroupCommand(
            group.Id, "Neu", false, true, [second, first]), TestContext.Current.CancellationToken);

        Assert.Same(group, dataAccess.SavedGroup);
        Assert.Equal("Neu", group.Name);
        Assert.False(group.IsTemporary);
        Assert.True(group.IsArchived);
        Assert.Equal(2, group.Players.Count);
        Assert.Equal(1, group.Players.Single(player => player.PlayerId == second).DefaultOrder);
        Assert.Equal(2, group.Players.Single(player => player.PlayerId == first).DefaultOrder);
    }

    [Fact]
    public async Task ExecuteAsync_CanMakeTemporaryGroupPermanent()
    {
        Group group = new("Einmalig", true, [Guid.NewGuid()]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess, new([]));

        await useCase.ExecuteAsync(new EditGroupCommand(
            group.Id, group.Name, true, false, [group.Players.Single().PlayerId]), TestContext.Current.CancellationToken);

        Assert.False(group.IsTemporary);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotMakePermanentGroupTemporary()
    {
        Group group = new("Stammgruppe", false, [Guid.NewGuid()]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess, new([]));

        await useCase.ExecuteAsync(new EditGroupCommand(
            group.Id, group.Name, false, false, [group.Players.Single().PlayerId]), TestContext.Current.CancellationToken);

        Assert.False(group.IsTemporary);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsOrderThatChangesPlayership()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Freitagabend", false, [first, second]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess, new([]));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(new EditGroupCommand(
                group.Id, group.Name, false, false, [first, Guid.NewGuid()]), TestContext.Current.CancellationToken));
    }

    private sealed class FakeEditGroupDataAccess(Group group) : IEditGroupDataAccess
    {
        public Group? SavedGroup { get; private set; }

        public Task<EditGroupDetails?> GetDetailsAsync(
            Guid groupId,
            string? gameKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EditGroupDetails?>(null);

        public Task<Group?> GetAggregateAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Group?>(groupId == group.Id ? group : null);

        public Task SaveAsync(
            Group groupToSave,
            GroupColorDefaults? colors,
            CancellationToken cancellationToken = default)
        {
            SavedGroup = groupToSave;
            return Task.CompletedTask;
        }
    }
}

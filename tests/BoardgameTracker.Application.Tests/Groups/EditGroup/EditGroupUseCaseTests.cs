using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Application.Tests.Groups.EditGroup;

public sealed class EditGroupUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ChangesEditablePropertiesWithoutChangingMembership()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Alt", false, [first, second]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);

        await useCase.ExecuteAsync(new EditGroupCommand(
            group.Id,
            "Neu",
            true,
            [second, first]));

        Assert.Same(group, dataAccess.SavedGroup);
        Assert.Equal("Neu", group.Name);
        Assert.True(group.IsTemporary);
        Assert.Equal(2, group.Members.Count);
        Assert.Equal(1, group.Members.Single(member => member.PersonId == second).DefaultOrder);
        Assert.Equal(2, group.Members.Single(member => member.PersonId == first).DefaultOrder);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsOrderThatChangesMembership()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Freitagabend", false, [first, second]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(new EditGroupCommand(
                group.Id,
                group.Name,
                group.IsTemporary,
                [first, Guid.NewGuid()])));
    }

    private sealed class FakeEditGroupDataAccess(Group group) : IEditGroupDataAccess
    {
        public Group? SavedGroup { get; private set; }

        public Task<EditGroupDetails?> GetDetailsAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EditGroupDetails?>(null);

        public Task<Group?> GetAggregateAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Group?>(groupId == group.Id ? group : null);

        public Task SaveAsync(
            Group groupToSave,
            CancellationToken cancellationToken = default)
        {
            SavedGroup = groupToSave;
            return Task.CompletedTask;
        }
    }
}

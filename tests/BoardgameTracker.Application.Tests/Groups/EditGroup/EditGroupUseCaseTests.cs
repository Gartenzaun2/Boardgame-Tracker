using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Application.Tests.Groups.EditGroup;

public sealed class EditGroupUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ChangesNameOrderAndArchiveStateWithoutChangingMembership()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Alt", false, [first, second]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);

        await useCase.ExecuteAsync(new EditGroupCommand(
            group.Id, "Neu", false, true, [second, first], new Dictionary<Guid, BoardgameTracker.Core.Games.ArkNova.ArkNovaPlayerColor?>()));

        Assert.Same(group, dataAccess.SavedGroup);
        Assert.Equal("Neu", group.Name);
        Assert.False(group.IsTemporary);
        Assert.True(group.IsArchived);
        Assert.Equal(2, group.Members.Count);
        Assert.Equal(1, group.Members.Single(member => member.PersonId == second).DefaultOrder);
        Assert.Equal(2, group.Members.Single(member => member.PersonId == first).DefaultOrder);
    }

    [Fact]
    public async Task ExecuteAsync_CanMakeTemporaryGroupPermanent()
    {
        Group group = new("Einmalig", true, [Guid.NewGuid()]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);

        await useCase.ExecuteAsync(new EditGroupCommand(
            group.Id, group.Name, true, false, [group.Members.Single().PersonId], new Dictionary<Guid, BoardgameTracker.Core.Games.ArkNova.ArkNovaPlayerColor?>()));

        Assert.False(group.IsTemporary);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotMakePermanentGroupTemporary()
    {
        Group group = new("Stammgruppe", false, [Guid.NewGuid()]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);

        await useCase.ExecuteAsync(new EditGroupCommand(
            group.Id, group.Name, false, false, [group.Members.Single().PersonId], new Dictionary<Guid, BoardgameTracker.Core.Games.ArkNova.ArkNovaPlayerColor?>()));

        Assert.False(group.IsTemporary);
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
                group.Id, group.Name, false, false, [first, Guid.NewGuid()], new Dictionary<Guid, BoardgameTracker.Core.Games.ArkNova.ArkNovaPlayerColor?>())));
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
            IReadOnlyDictionary<Guid, BoardgameTracker.Core.Games.ArkNova.ArkNovaPlayerColor?> arkNovaDefaultColors,
            CancellationToken cancellationToken = default)
        {
            SavedGroup = groupToSave;
            return Task.CompletedTask;
        }
    }
}

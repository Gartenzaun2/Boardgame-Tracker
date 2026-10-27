using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Games.ArkNova;

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
            group.Id, "Neu", false, true, [second, first], NoColors(group)));

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
            group.Id, group.Name, true, false, [group.Members.Single().PersonId], NoColors(group)));

        Assert.False(group.IsTemporary);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotMakePermanentGroupTemporary()
    {
        Group group = new("Stammgruppe", false, [Guid.NewGuid()]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);

        await useCase.ExecuteAsync(new EditGroupCommand(
            group.Id, group.Name, false, false, [group.Members.Single().PersonId], NoColors(group)));

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
                group.Id, group.Name, false, false, [first, Guid.NewGuid()], NoColors(group))));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsDefaultColorForUnknownMember()
    {
        Group group = new("Freitagabend", false, [Guid.NewGuid()]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);
        Dictionary<Guid, ArkNovaPlayerColor?> colors = NoColors(group);
        colors.Remove(group.Members.Single().PersonId);
        colors.Add(Guid.NewGuid(), ArkNovaPlayerColor.Blue);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(
            new EditGroupCommand(
                group.Id,
                group.Name,
                false,
                false,
                [group.Members.Single().PersonId],
                colors)));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsDuplicateDefaultColors()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Group group = new("Freitagabend", false, [first, second]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);
        Dictionary<Guid, ArkNovaPlayerColor?> colors = new()
        {
            [first] = ArkNovaPlayerColor.Blue,
            [second] = ArkNovaPlayerColor.Blue
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(
            new EditGroupCommand(group.Id, group.Name, false, false, [first, second], colors)));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsUndefinedDefaultColor()
    {
        Guid personId = Guid.NewGuid();
        Group group = new("Freitagabend", false, [personId]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);
        Dictionary<Guid, ArkNovaPlayerColor?> colors = new()
        {
            [personId] = (ArkNovaPlayerColor)999
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => useCase.ExecuteAsync(
            new EditGroupCommand(group.Id, group.Name, false, false, [personId], colors)));
    }

    private static Dictionary<Guid, ArkNovaPlayerColor?> NoColors(Group group) =>
        group.Members.ToDictionary(member => member.PersonId, _ => (ArkNovaPlayerColor?)null);

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
            IReadOnlyDictionary<Guid, ArkNovaPlayerColor?> arkNovaDefaultColors,
            CancellationToken cancellationToken = default)
        {
            SavedGroup = groupToSave;
            return Task.CompletedTask;
        }
    }
}

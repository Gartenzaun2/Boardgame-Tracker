using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.Players;

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
        EditGroupUseCase useCase = new(dataAccess);

        await useCase.ExecuteAsync(new EditGroupCommand(
            group.Id, "Neu", false, true, [second, first], NoColors(group)));

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
        EditGroupUseCase useCase = new(dataAccess);

        await useCase.ExecuteAsync(new EditGroupCommand(
            group.Id, group.Name, true, false, [group.Players.Single().PlayerId], NoColors(group)));

        Assert.False(group.IsTemporary);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotMakePermanentGroupTemporary()
    {
        Group group = new("Stammgruppe", false, [Guid.NewGuid()]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);

        await useCase.ExecuteAsync(new EditGroupCommand(
            group.Id, group.Name, false, false, [group.Players.Single().PlayerId], NoColors(group)));

        Assert.False(group.IsTemporary);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsOrderThatChangesPlayership()
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
    public async Task ExecuteAsync_RejectsDefaultColorForUnknownPlayer()
    {
        Group group = new("Freitagabend", false, [Guid.NewGuid()]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);
        Dictionary<Guid, ArkNovaPlayerColor?> colors = NoColors(group);
        colors.Remove(group.Players.Single().PlayerId);
        colors.Add(Guid.NewGuid(), ArkNovaPlayerColor.Blue);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(
            new EditGroupCommand(
                group.Id,
                group.Name,
                false,
                false,
                [group.Players.Single().PlayerId],
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
        Guid playerId = Guid.NewGuid();
        Group group = new("Freitagabend", false, [playerId]);
        FakeEditGroupDataAccess dataAccess = new(group);
        EditGroupUseCase useCase = new(dataAccess);
        Dictionary<Guid, ArkNovaPlayerColor?> colors = new()
        {
            [playerId] = (ArkNovaPlayerColor)999
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => useCase.ExecuteAsync(
            new EditGroupCommand(group.Id, group.Name, false, false, [playerId], colors)));
    }

    private static Dictionary<Guid, ArkNovaPlayerColor?> NoColors(Group group) =>
        group.Players.ToDictionary(player => player.PlayerId, _ => (ArkNovaPlayerColor?)null);

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

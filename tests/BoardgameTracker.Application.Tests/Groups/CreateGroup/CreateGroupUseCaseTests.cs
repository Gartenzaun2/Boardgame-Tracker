using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Application.Groups.CreateGroup;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Tests.Groups.CreateGroup;

public sealed class CreateGroupUseCaseTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExecuteAsync_CreatesGroupWithExistingAndNewPlayersInRequestedOrder()
    {
        Guid existingPlayerId = Guid.NewGuid();
        FakeCreateGroupDataAccess dataAccess = new(new HashSet<Guid> { existingPlayerId });
        CreateGroupUseCase useCase = new(dataAccess, new([]));

        Guid groupId = await useCase.ExecuteAsync(new CreateGroupCommand(
            "Freitagabend",
            false,
            [
                new CreateGroupPlayerInput(existingPlayerId, "Anna", false),
                new CreateGroupPlayerInput(null, "Ben", true)
            ]), Token);

        Assert.NotEqual(Guid.Empty, groupId);
        Assert.NotNull(dataAccess.SavedGroup);
        Assert.Single(dataAccess.SavedNewPlayers);
        Assert.True(dataAccess.SavedNewPlayers.Single().IsGuest);

        Group savedGroup = dataAccess.SavedGroup!;
        Assert.Collection(
            savedGroup.Players.OrderBy(player => player.DefaultOrder),
            first => Assert.Equal(existingPlayerId, first.PlayerId),
            second => Assert.Equal(dataAccess.SavedNewPlayers.Single().Id, second.PlayerId));
    }

    [Fact]
    public async Task ExecuteAsync_RejectsInactiveOrMissingExistingPlayer()
    {
        Guid playerId = Guid.NewGuid();
        FakeCreateGroupDataAccess dataAccess = new(new HashSet<Guid>());
        CreateGroupUseCase useCase = new(dataAccess, new([]));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new CreateGroupCommand(
                "Freitagabend",
                false,
                [new CreateGroupPlayerInput(playerId, "Anna", false)]), Token));
    }

    private sealed class FakeCreateGroupDataAccess(IReadOnlySet<Guid> activeIds)
        : ICreateGroupDataAccess
    {
        public Group? SavedGroup { get; private set; }
        public IReadOnlyCollection<Player> SavedNewPlayers { get; private set; } = [];

        public Task<IReadOnlyList<SelectablePlayer>> GetSelectablePlayersAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SelectablePlayer>>([]);

        public Task<IReadOnlySet<Guid>> GetActivePlayerIdsAsync(
            IReadOnlyCollection<Guid> playerIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(activeIds);

        public Task SaveAsync(
            Group group,
            IReadOnlyCollection<Player> newPlayers,
            GroupColorDefaults? colors,
            CancellationToken cancellationToken = default)
        {
            SavedGroup = group;
            SavedNewPlayers = newPlayers;
            return Task.CompletedTask;
        }
    }
}

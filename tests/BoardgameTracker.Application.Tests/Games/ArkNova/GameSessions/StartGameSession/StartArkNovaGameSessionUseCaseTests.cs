using BoardgameTracker.Application.Games.ArkNova.GameSessions.StartGameSession;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.GameSessions.StartGameSession;

public sealed class StartArkNovaGameSessionUseCaseTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExecuteAsync_PersistsGameSessionAndArkNovaSnapshots()
    {
        Guid groupId = Guid.NewGuid();
        Guid gameId = Guid.NewGuid();
        Guid anna = Guid.NewGuid();
        Guid ben = Guid.NewGuid();
        FakeDataAccess dataAccess = new(new StartArkNovaGameSessionContext(
            gameId,
            false,
            false,
            [ben, anna],
            Enumerable.Range(1, 14).ToHashSet()));
        StartArkNovaGameSessionUseCase useCase = new(dataAccess);
        DateTimeOffset startedAt = new(2026, 10, 27, 10, 0, 0, TimeSpan.Zero);

        Guid gameSessionId = await useCase.ExecuteAsync(new StartArkNovaGameSessionCommand(
            groupId,
            startedAt,
            [
                new(anna, PlayerColor.Blue, 1, 2, 1),
                new(ben, PlayerColor.Red, 3, 4, 4)
            ]), Token);

        Assert.NotEqual(Guid.Empty, gameSessionId);
        Assert.NotNull(dataAccess.SavedGameSession);
        Assert.Equal(groupId, dataAccess.SavedGameSession!.GroupId);
        Assert.Equal(gameId, dataAccess.SavedGameSession.GameId);
        Assert.Equal(GameSessionStatus.Running, dataAccess.SavedGameSession.Status);
        Assert.Equal(new[] { anna, ben }, dataAccess.SavedGameSession.Players.OrderBy(player => player.Order).Select(player => player.PlayerId));
        Assert.Equal(2, dataAccess.SavedArkNovaPlayers.Count);
        Assert.All(dataAccess.SavedArkNovaPlayers, player => Assert.Equal(gameSessionId, player.GameSessionId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlayersDoNotMatchGroup_Throws()
    {
        Guid groupId = Guid.NewGuid();
        Guid anna = Guid.NewGuid();
        Guid ben = Guid.NewGuid();
        Guid chris = Guid.NewGuid();
        FakeDataAccess dataAccess = new(new StartArkNovaGameSessionContext(
            Guid.NewGuid(),
            false,
            false,
            [anna, ben],
            Enumerable.Range(1, 14).ToHashSet()));
        StartArkNovaGameSessionUseCase useCase = new(dataAccess);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(
            new StartArkNovaGameSessionCommand(
                groupId,
                DateTimeOffset.UtcNow,
                [
                    new(anna, PlayerColor.Blue, 1, 2, 1),
                    new(chris, PlayerColor.Red, 3, 4, 3)
                ]), Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenGroupIsArchived_Throws()
    {
        Guid groupId = Guid.NewGuid();
        Guid anna = Guid.NewGuid();
        FakeDataAccess dataAccess = new(new StartArkNovaGameSessionContext(
            Guid.NewGuid(),
            true,
            false,
            [anna],
            Enumerable.Range(1, 14).ToHashSet()));
        StartArkNovaGameSessionUseCase useCase = new(dataAccess);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(
            new StartArkNovaGameSessionCommand(
                groupId,
                DateTimeOffset.UtcNow,
                [new(anna, PlayerColor.Blue, 1, 2, 1)]), Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenZooPlanIsUnknown_Throws()
    {
        Guid groupId = Guid.NewGuid();
        Guid anna = Guid.NewGuid();
        FakeDataAccess dataAccess = new(new StartArkNovaGameSessionContext(
            Guid.NewGuid(),
            false,
            false,
            [anna],
            Enumerable.Range(1, 14).ToHashSet()));
        StartArkNovaGameSessionUseCase useCase = new(dataAccess);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(
            new StartArkNovaGameSessionCommand(
                groupId,
                DateTimeOffset.UtcNow,
                [new(anna, PlayerColor.Blue, 1, 99, 1)]), Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnotherGameSessionIsOpen_Throws()
    {
        Guid groupId = Guid.NewGuid();
        Guid anna = Guid.NewGuid();
        FakeDataAccess dataAccess = new(new StartArkNovaGameSessionContext(
            Guid.NewGuid(),
            false,
            true,
            [anna],
            Enumerable.Range(1, 14).ToHashSet()));
        StartArkNovaGameSessionUseCase useCase = new(dataAccess);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(
            new StartArkNovaGameSessionCommand(
                groupId,
                DateTimeOffset.UtcNow,
                [new(anna, PlayerColor.Blue, 1, 2, 1)]), Token));
    }

    private sealed class FakeDataAccess(StartArkNovaGameSessionContext context)
        : IStartArkNovaGameSessionDataAccess
    {
        public GameSession? SavedGameSession { get; private set; }
        public IReadOnlyCollection<ArkNovaGameSessionPlayer> SavedArkNovaPlayers { get; private set; } = [];

        public Task<StartArkNovaGameSessionContext?> LoadContextAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StartArkNovaGameSessionContext?>(context);

        public Task SaveAsync(
            GameSession gameSession,
            IReadOnlyCollection<ArkNovaGameSessionPlayer> arkNovaPlayers,
            CancellationToken cancellationToken = default)
        {
            SavedGameSession = gameSession;
            SavedArkNovaPlayers = [.. arkNovaPlayers];
            return Task.CompletedTask;
        }
    }
}

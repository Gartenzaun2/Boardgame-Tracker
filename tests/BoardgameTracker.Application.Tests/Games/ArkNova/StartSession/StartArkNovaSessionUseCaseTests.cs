using BoardgameTracker.Application.Games.ArkNova.StartSession;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.Setup;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.StartSession;

public sealed class StartArkNovaSessionUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_PersistsSessionAndArkNovaSnapshots()
    {
        Guid groupId = Guid.NewGuid();
        Guid gameId = Guid.NewGuid();
        Guid anna = Guid.NewGuid();
        Guid ben = Guid.NewGuid();
        var dataAccess = new FakeDataAccess(new StartArkNovaSessionContext(
            gameId,
            false,
            false,
            [anna, ben],
            Enumerable.Range(1, 14).ToHashSet()));
        var useCase = new StartArkNovaSessionUseCase(dataAccess);
        DateTimeOffset startedAt = new(2026, 10, 27, 10, 0, 0, TimeSpan.Zero);

        Guid sessionId = await useCase.ExecuteAsync(new StartArkNovaSessionCommand(
            groupId,
            startedAt,
            [
                new(anna, ArkNovaPlayerColor.Blue, 1, 2, 1),
                new(ben, ArkNovaPlayerColor.Red, 3, 4, 4)
            ]));

        Assert.NotEqual(Guid.Empty, sessionId);
        Assert.NotNull(dataAccess.SavedSession);
        Assert.Equal(groupId, dataAccess.SavedSession!.GroupId);
        Assert.Equal(gameId, dataAccess.SavedSession.GameId);
        Assert.Equal(GameSessionStatus.Running, dataAccess.SavedSession.Status);
        Assert.Equal(new[] { anna, ben }, dataAccess.SavedSession.Players.OrderBy(player => player.Order).Select(player => player.PersonId));
        Assert.Equal(2, dataAccess.SavedArkNovaPlayers.Count);
        Assert.All(dataAccess.SavedArkNovaPlayers, player => Assert.Equal(sessionId, player.GameSessionId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPlayersDoNotMatchGroup_Throws()
    {
        Guid groupId = Guid.NewGuid();
        Guid anna = Guid.NewGuid();
        Guid ben = Guid.NewGuid();
        Guid chris = Guid.NewGuid();
        var dataAccess = new FakeDataAccess(new StartArkNovaSessionContext(
            Guid.NewGuid(),
            false,
            false,
            [anna, ben],
            Enumerable.Range(1, 14).ToHashSet()));
        var useCase = new StartArkNovaSessionUseCase(dataAccess);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(
            new StartArkNovaSessionCommand(
                groupId,
                DateTimeOffset.UtcNow,
                [
                    new(anna, ArkNovaPlayerColor.Blue, 1, 2, 1),
                    new(chris, ArkNovaPlayerColor.Red, 3, 4, 3)
                ])));
    }


    [Fact]
    public async Task ExecuteAsync_WhenAnotherSessionIsOpen_Throws()
    {
        Guid groupId = Guid.NewGuid();
        Guid anna = Guid.NewGuid();
        var dataAccess = new FakeDataAccess(new StartArkNovaSessionContext(
            Guid.NewGuid(),
            false,
            true,
            [anna],
            Enumerable.Range(1, 14).ToHashSet()));
        var useCase = new StartArkNovaSessionUseCase(dataAccess);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(
            new StartArkNovaSessionCommand(
                groupId,
                DateTimeOffset.UtcNow,
                [new(anna, ArkNovaPlayerColor.Blue, 1, 2, 1)])));
    }

    private sealed class FakeDataAccess(StartArkNovaSessionContext context)
        : IStartArkNovaSessionDataAccess
    {
        public GameSession? SavedSession { get; private set; }
        public IReadOnlyCollection<ArkNovaSessionPlayer> SavedArkNovaPlayers { get; private set; } = [];

        public Task<StartArkNovaSessionContext?> LoadContextAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StartArkNovaSessionContext?>(context);

        public Task SaveAsync(
            GameSession session,
            IReadOnlyCollection<ArkNovaSessionPlayer> arkNovaPlayers,
            CancellationToken cancellationToken = default)
        {
            SavedSession = session;
            SavedArkNovaPlayers = arkNovaPlayers.ToArray();
            return Task.CompletedTask;
        }
    }
}

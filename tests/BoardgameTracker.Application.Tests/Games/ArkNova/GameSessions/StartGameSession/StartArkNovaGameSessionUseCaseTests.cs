using BoardgameTracker.Application.Games.ArkNova.GameSessions.StartGameSession;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.GameSessions.StartGameSession;

public sealed class StartArkNovaGameSessionUseCaseTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static StartArkNovaGameSessionCommand Command(SliceDatabase database) => new(database.Group.Id, DateTimeOffset.UtcNow,
        [new(database.First.Id, PlayerColor.Blue, 1, 2, 1), new(database.Second.Id, PlayerColor.Red, 3, 4, 4)]);

    [Fact]
    public async Task StartAsync_PersistsGameSessionAndArkNovaSnapshots()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Guid id = await new StartArkNovaGameSessionUseCase(database).StartAsync(Command(database), Token);
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        GameSession saved = await db.GameSessions.Include(session => session.Players).SingleAsync(Token);
        Assert.Equal(id, saved.Id);
        Assert.Equal(database.Group.Id, saved.GroupId);
        Assert.Equal(database.GameId, saved.GameId);
        Assert.Equal(GameSessionStatus.Running, saved.Status);
        Assert.Equal(new[] { database.First.Id, database.Second.Id }, saved.Players.OrderBy(player => player.Order).Select(player => player.PlayerId));
        Assert.Equal(2, await db.ArkNovaGameSessionPlayers.CountAsync(player => player.GameSessionId == id, Token));
        Assert.Equal(4, (await db.ArkNovaGameSessionPlayers.SingleAsync(player => player.PlayerId == database.Second.Id, Token)).SelectedZooPlanNumber);
    }

    [Theory]
    [InlineData("membership")]
    [InlineData("archived")]
    [InlineData("plan")]
    [InlineData("open")]
    public async Task StartAsync_RejectsInvalidPersistedContextWithoutWrites(string reason)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync(archived: reason == "archived");
        StartArkNovaGameSessionCommand command = Command(database);
        if (reason == "membership")
            command = command with { Players = [new(Guid.NewGuid(), PlayerColor.Blue, 1, 2, 1), command.Players[1]] };
        if (reason == "plan")
            command = command with { Players = [new(database.First.Id, PlayerColor.Blue, 1, 99, 1), command.Players[1]] };
        if (reason == "open")
            await database.AddSessionAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new StartArkNovaGameSessionUseCase(database).StartAsync(command, Token));
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        Assert.Equal(reason == "open" ? 1 : 0, await db.GameSessions.CountAsync(Token));
        Assert.Empty(await db.ArkNovaGameSessionPlayers.ToArrayAsync(Token));
    }
}

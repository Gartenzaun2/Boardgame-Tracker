using BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.GameSessions.StartGameSession;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.GameSessions.StartGameSession;

public sealed class SqliteStartArkNovaGameSessionStoreTests
{
    [Theory]
    [InlineData(true, true, GameSessionStatus.Running, true)]
    [InlineData(true, true, GameSessionStatus.Scoring, true)]
    [InlineData(true, true, GameSessionStatus.Completed, false)]
    [InlineData(false, true, GameSessionStatus.Running, false)]
    [InlineData(true, false, GameSessionStatus.Scoring, false)]
    [InlineData(false, false, GameSessionStatus.Running, false)]
    public async Task LoadContextAsync_OnlyBlocksOpenSessionsForSameGameAndGroup(
        bool sameGame, bool sameGroup, GameSessionStatus status, bool expected)
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<BoardgameTrackerDbContext>()
            .UseSqlite(connection).Options;
        await using var context = new BoardgameTrackerDbContext(options);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        Player player = new("Anna");
        Group group = new("Target", false, [player.Id]);
        Group otherGroup = new("Other", false, [player.Id]);
        Game game = new(ArkNovaGame.Key);
        Game otherGame = new("other-game");
        context.AddRange(player, group, otherGroup, game, otherGame);
        var startedAt = DateTimeOffset.UtcNow;
        GameSession session = new(sameGame ? game.Id : otherGame.Id,
            sameGroup ? group.Id : otherGroup.Id, startedAt, [player.Id]);
        if (status != GameSessionStatus.Running)
            session.BeginScoring(startedAt.AddMinutes(1));
        if (status == GameSessionStatus.Completed)
            session.Complete(startedAt.AddMinutes(2));
        context.GameSessions.Add(session);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dataAccess = new SqliteStartArkNovaGameSessionStore(new ContextFactory(options));
        var result = await dataAccess.LoadContextAsync(group.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(expected, result.HasOpenGameSession);
    }

    private sealed class ContextFactory(DbContextOptions<BoardgameTrackerDbContext> options)
        : IDbContextFactory<BoardgameTrackerDbContext>
    {
        public BoardgameTrackerDbContext CreateDbContext() => new(options);
    }
}

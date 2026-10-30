using BoardgameTracker.App.Persistence.SQLite;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Application.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Persistence;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public async Task Initialize_CreatesSchemaBeforeSteps_AndAwaitsStepsInOrderWithSameContext()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync(token);
        ContextFactory factory = new(connection);
        BoardgameTrackerDbContext? firstContext = null;
        Player player = new("Anna");

        InitializationStep first = new(async (db, cancellationToken) =>
        {
            Assert.Equal(token, cancellationToken);
            Assert.Empty(await db.Players.ToListAsync(cancellationToken));
            firstContext = db;
            await Task.Yield();
            db.Players.Add(player);
            await db.SaveChangesAsync(cancellationToken);
        });
        InitializationStep second = new(async (db, cancellationToken) =>
        {
            Assert.Same(firstContext, db);
            Assert.Equal(token, cancellationToken);
            Assert.Equal(player.Id, (await db.Players.SingleAsync(cancellationToken)).Id);
        });

        await new DatabaseInitializer(factory, [first, second]).InitializeAsync(token);

        Assert.NotNull(firstContext);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => firstContext.Players.ToListAsync(token));
    }

    [Fact]
    public async Task Initialize_PropagatesStepFailure_AndSkipsFollowingSteps()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync(token);
        ContextFactory factory = new(connection);
        InvalidOperationException failure = new("Initialization failed.");
        bool followingStepExecuted = false;
        InitializationStep first = new((_, _) => Task.FromException(failure));
        InitializationStep second = new((_, _) =>
        {
            followingStepExecuted = true;
            return Task.CompletedTask;
        });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new DatabaseInitializer(factory, [first, second]).InitializeAsync(token));

        Assert.Same(failure, actual);
        Assert.False(followingStepExecuted);
    }

    private sealed class InitializationStep(
        Func<BoardgameTrackerDbContext, CancellationToken, Task> execute) : IDatabaseInitializationStep
    {
        public Task ExecuteAsync(BoardgameTrackerDbContext dbContext, CancellationToken cancellationToken = default) =>
            execute(dbContext, cancellationToken);
    }

    private sealed class ContextFactory(SqliteConnection connection) : IDbContextFactory<BoardgameTrackerDbContext>
    {
        public BoardgameTrackerDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().UseTestSqlite(connection).Options);
    }
}

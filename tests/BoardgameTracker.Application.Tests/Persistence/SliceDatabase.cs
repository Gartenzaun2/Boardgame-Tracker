using BoardgameTracker.App.Persistence.SQLite;
using BoardgameTracker.Application.Games.ArkNova.Persistence.Seeding;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BoardgameTracker.Application.Tests.Persistence;

internal static class TestSqliteOptions
{
    public static DbContextOptionsBuilder<T> UseTestSqlite<T>(this DbContextOptionsBuilder<T> options, SqliteConnection connection)
        where T : DbContext
    {
        // Supplied open connections do not trigger ConnectionOpened. Use the host's registration.
        SqliteNameCollationInterceptor.Register(connection);
        options.UseSqlite(connection).AddInterceptors(new SqliteNameCollationInterceptor())
            .ConfigureWarnings(warnings => warnings.Throw(CoreEventId.ShadowPropertyCreated));
        return options;
    }
}

internal sealed class SliceDatabase : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public Player First { get; } = new("Anna");
    public Player Second { get; } = new("Ben");
    public Group Group { get; private set; } = null!;
    public Guid GameId { get; private set; }
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public BoardgameTrackerDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().UseTestSqlite(_connection).Options);

    public static async Task<SliceDatabase> CreateAsync(bool temporary = false, bool archived = false)
    {
        SliceDatabase database = new();
        await database._connection.OpenAsync(Token);
        await new DatabaseInitializer(database, [new ArkNovaReferenceDataSeeder()]).InitializeAsync(Token);
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        database.GameId = await db.Games.Where(game => game.Key == ArkNovaGame.Key).Select(game => game.Id).SingleAsync(Token);
        database.Group = new("Freitag", temporary, [database.First.Id, database.Second.Id]);
        database.Group.SetArchived(archived);
        db.AddRange(database.First, database.Second, database.Group);
        await db.SaveChangesAsync(Token);
        return database;
    }

    public async Task<GameSession> AddSessionAsync(bool scoring = false)
    {
        GameSession session = new(GameId, Group.Id, DateTimeOffset.UtcNow.AddHours(-2), [First.Id, Second.Id]);
        if (scoring)
            session.BeginScoring(session.StartedAt.AddHours(1));
        await using BoardgameTrackerDbContext db = CreateDbContext();
        db.GameSessions.Add(session);
        await db.SaveChangesAsync(Token);
        return session;
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}

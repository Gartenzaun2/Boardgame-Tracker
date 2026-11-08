using BoardgameTracker.App.Persistence.SQLite;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.Players;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Persistence;

public sealed class SqliteProviderTests
{
    [Fact]
    public async Task HostOptions_RegisterNameCollationForCreatedAndReopenedConnections()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Guid.NewGuid().ToString(),
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        }.ConnectionString;
        // The anchor only keeps the isolated database alive; EF creates its own connections.
        await using SqliteConnection anchor = new(connectionString);
        await anchor.OpenAsync(token);
        DbContextOptionsBuilder<BoardgameTrackerDbContext> options = new();
        SqliteServiceCollectionExtensions.Configure(options, connectionString);
        await using (BoardgameTrackerDbContext db = new(options.Options))
        {
            db.Database.EnsureCreated();
            db.Players.Add(new("Änna"));
            await db.SaveChangesAsync(token);
        }
        await using (BoardgameTrackerDbContext db = new(options.Options))
        {
            await db.Database.OpenConnectionAsync(token);
            Assert.Equal("Änna", (await db.Players.SingleAsync(player => player.Name == " änNA ", token)).Name);
            db.Players.Add(new(" änNA "));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(token));
            await db.Database.CloseConnectionAsync();
            await db.Database.OpenConnectionAsync(token);
            Assert.Equal(1, await db.Players.CountAsync(token));
        }
    }
}

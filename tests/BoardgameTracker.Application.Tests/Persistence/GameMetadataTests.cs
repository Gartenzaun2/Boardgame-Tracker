using BoardgameTracker.Application.Games;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Application.Games.ArkNova.Persistence.Seeding;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Persistence;

public sealed class GameMetadataTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FreshSchema_AndRepeatedInitializationSynchronizeMetadataWithoutReplacingReferences()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await new AddGroupGameUseCase(database)
            .AddGroupGameAsync(database.Group.Id, database.GameId, Token);
        GameSession session = await database.AddSessionAsync();
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
        {
            Game game = await db.Games.SingleAsync(Token);
            Assert.Equal(ArkNovaGame.Name, game.Name);
            Assert.Equal(ArkNovaGame.BadgeText, game.BadgeText);
            Assert.Equal(ArkNovaGame.MinPlayers, game.MinPlayers);
            Assert.Equal(ArkNovaGame.MaxPlayers, game.MaxPlayers);
            game.SetMetadata("Outdated", "Old", 2, 8);
            await db.SaveChangesAsync(Token);
        }

        DatabaseInitializer initializer = new(database, [new ArkNovaReferenceDataSeeder("Registered name", "Badge")]);
        await initializer.InitializeAsync(Token);
        await initializer.InitializeAsync(Token);

        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Game stored = Assert.Single(await verify.Games.ToArrayAsync(Token));
        Assert.Equal(database.GameId, stored.Id);
        Assert.Equal(ArkNovaGame.Key, stored.Key);
        Assert.Equal("Registered name", stored.Name);
        Assert.Equal("Badge", stored.BadgeText);
        Assert.Equal(ArkNovaGame.MinPlayers, stored.MinPlayers);
        Assert.Equal(ArkNovaGame.MaxPlayers, stored.MaxPlayers);
        Assert.Equal(database.GameId, (await verify.GameSessions.SingleAsync(Token)).GameId);
        Assert.Equal(session.Id, (await verify.GameSessions.SingleAsync(Token)).Id);
        Assert.Equal(database.GameId, (await verify.GroupGames.SingleAsync(Token)).GameId);
        Assert.Equal(2, await verify.Players.CountAsync(Token));
        Assert.Single(await verify.Groups.ToArrayAsync(Token));
        PlayOverviewQuery query = new(database, new TestGameActivationStore());
        Assert.Equal("Registered name", (await query.GetGameAsync(ArkNovaGame.Key, Token)).Name);
        Assert.Equal("Badge", Assert.Single(await query.ListAsync(Token)).BadgeText);
        Assert.Equal("Registered name", Assert.Single(await new GameActivationUseCase(database, new TestGameActivationStore()).GetGamesAsync(Token)).Name);
    }

    [Theory]
    [InlineData("UPDATE Games SET MinPlayers = 0")]
    [InlineData("UPDATE Games SET MaxPlayers = MinPlayers - 1")]
    [InlineData("UPDATE Games SET Name = ''")]
    [InlineData("UPDATE Games SET Name = ' untrimmed '")]
    [InlineData("UPDATE Games SET Name = printf('%101s', 'x')")]
    [InlineData("UPDATE Games SET BadgeText = ''")]
    [InlineData("UPDATE Games SET BadgeText = ' untrimmed '")]
    [InlineData("UPDATE Games SET Key = ''")]
    [InlineData("UPDATE Games SET Key = printf('%101s', 'x')")]
    public async Task FreshSchema_RejectsInvalidMetadataAtDatabaseBoundary(string sql)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync(sql, Token));
    }

    [Fact]
    public async Task Persistence_RejectsDuplicateAndChangedKeys()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        db.Games.Add(new(ArkNovaGame.Key, "Other", "O", 1, 2));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Token));
        db.ChangeTracker.Clear();
        Game game = await db.Games.SingleAsync(Token);
        db.Entry(game).Property(candidate => candidate.Key).CurrentValue = "changed";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(Token));
    }
}

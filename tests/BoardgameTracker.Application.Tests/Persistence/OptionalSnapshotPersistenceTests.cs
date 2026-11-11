using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Persistence;

public sealed class OptionalSnapshotPersistenceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UnorderedColorlessSession_RoundtripsWithoutCreatingOrderColorsOrChangingGroupDefaults()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        GameSession session;
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
        {
            Game game = new("without-colors", "Without colors", "WC", 1, 8);
            Group group = await db.Groups.Include(group => group.Players).SingleAsync(Token);
            session = new(game.Id, group.Id, DateTimeOffset.UtcNow,
                [database.Second.Id, database.First.Id], recordStartingOrder: false);
            group.ApplyStartingOrder(session);
            db.AddRange(game, session);
            await db.SaveChangesAsync(Token);
        }
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        GameSession stored = await verify.GameSessions.Include(session => session.Players).SingleAsync(Token);
        Assert.False(stored.HasStartingOrder);
        Assert.All(stored.Players, player => Assert.Null(player.Order));
        Assert.Equal(new[] { database.First.Id, database.Second.Id }, await verify.GroupPlayers.OrderBy(player => player.DefaultOrder)
            .Select(player => player.PlayerId).ToArrayAsync(Token));
        Assert.Empty(await verify.GameSessionPlayerColors.ToArrayAsync(Token));
        Assert.Empty(await verify.GroupGamePlayerColors.ToArrayAsync(Token));
    }

    [Theory]
    [InlineData("group-outsider")]
    [InlineData("group-key")]
    [InlineData("session-key")]
    [InlineData("session-missing")]
    [InlineData("group-game")]
    [InlineData("group-duplicate")]
    [InlineData("session-outsider")]
    [InlineData("session-duplicate")]
    public async Task ColorConstraints_RejectInvalidReferencesAndDuplicateColors(string invalid)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        GameSession session = await database.AddSessionAsync();
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        Player outsider = new("Outsider");
        db.Players.Add(outsider);
        db.GroupGamePlayerColors.Add(new(database.Group.Id, database.GameId, database.First.Id, PlayerColor.Blue));
        db.GameSessionPlayerColors.Add(new(session.Id, database.First.Id, PlayerColor.Blue));
        await db.SaveChangesAsync(Token);
        db.ChangeTracker.Clear();
        switch (invalid)
        {
            case "group-outsider":
                db.GroupGamePlayerColors.Add(new(database.Group.Id, database.GameId, outsider.Id, PlayerColor.Red));
                break;
            case "group-key":
                db.GroupGamePlayerColors.Add(new(database.Group.Id, database.GameId, database.First.Id, PlayerColor.Red));
                break;
            case "session-key":
                db.GameSessionPlayerColors.Add(new(session.Id, database.First.Id, PlayerColor.Red));
                break;
            case "session-missing":
                db.GameSessionPlayerColors.Add(new(Guid.NewGuid(), database.First.Id, PlayerColor.Red));
                break;
            case "group-game":
                db.GroupGamePlayerColors.Add(new(database.Group.Id, Guid.NewGuid(), database.Second.Id, PlayerColor.Red));
                break;
            case "group-duplicate":
                db.GroupGamePlayerColors.Add(new(database.Group.Id, database.GameId, database.Second.Id, PlayerColor.Blue));
                break;
            case "session-outsider":
                db.GameSessionPlayerColors.Add(new(session.Id, outsider.Id, PlayerColor.Red));
                break;
            default:
                db.GameSessionPlayerColors.Add(new(session.Id, database.Second.Id, PlayerColor.Blue));
                break;
        }
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Token));
    }

    [Fact]
    public async Task Colors_AreScopedByGameAndSessionAndDefaultsDoNotChangeHistory()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        GameSession first = await database.AddSessionAsync();
        GameSession second = await database.AddSessionAsync();
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
        {
            Game other = new("other", "Other", "O", 1, 8);
            db.Games.Add(other);
            db.GroupGamePlayerColors.AddRange(
                new(database.Group.Id, database.GameId, database.First.Id, PlayerColor.Blue),
                new(database.Group.Id, other.Id, database.Second.Id, PlayerColor.Blue));
            db.GameSessionPlayerColors.AddRange(new(first.Id, database.First.Id, PlayerColor.Blue),
                new(second.Id, database.Second.Id, PlayerColor.Blue));
            await db.SaveChangesAsync(Token);
            (await db.GroupGamePlayerColors.SingleAsync(color => color.GameId == database.GameId, Token)).SetDefaultColor(PlayerColor.Red);
            await db.SaveChangesAsync(Token);
        }
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.All(await verify.GameSessionPlayerColors.ToArrayAsync(Token), color => Assert.Equal(PlayerColor.Blue, color.Color));
        Assert.Equal(2, await verify.GroupGamePlayerColors.CountAsync(Token));
    }

    [Theory]
    [InlineData("UPDATE GroupGamePlayerColors SET DefaultColor = 0")]
    [InlineData("UPDATE GroupGamePlayerColors SET DefaultColor = 13")]
    [InlineData("UPDATE GameSessionPlayerColors SET Color = 0")]
    [InlineData("UPDATE GameSessionPlayerColors SET Color = 13")]
    [InlineData("UPDATE GameSessionPlayers SET \"Order\" = 0")]
    public async Task Schema_RejectsInvalidColorAndOrderValues(string sql)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        GameSession session = await database.AddSessionAsync();
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        db.GroupGamePlayerColors.Add(new(database.Group.Id, database.GameId, database.First.Id, PlayerColor.Blue));
        db.GameSessionPlayerColors.Add(new(session.Id, database.First.Id, PlayerColor.Blue));
        await db.SaveChangesAsync(Token);
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync(sql, Token));
    }
}

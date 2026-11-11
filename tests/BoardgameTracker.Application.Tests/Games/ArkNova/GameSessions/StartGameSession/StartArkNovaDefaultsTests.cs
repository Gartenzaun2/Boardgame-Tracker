using BoardgameTracker.Application.Games.ArkNova.GameSessions;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova.Groups;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.GameSessions.StartGameSession;

public sealed class StartArkNovaDefaultsTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static StartArkNovaGameSessionCommand Command(SliceDatabase database) =>
        new(database.Group.Id, DateTimeOffset.UtcNow,
            [new(database.Second.Id, PlayerColor.Blue, 2),
             new(database.First.Id, PlayerColor.Red, 3)]);

    private static async Task SeedColorsAsync(SliceDatabase database)
    {
        await using var db = database.CreateDbContext();
        db.ArkNovaGroupPlayers.AddRange(
            new ArkNovaGroupPlayer(database.Group.Id, database.First.Id, PlayerColor.Blue),
            new ArkNovaGroupPlayer(database.Group.Id, database.Second.Id, PlayerColor.Red));
        await db.SaveChangesAsync(Token);
    }

    [Fact]
    public async Task ConsecutiveStarts_UpdateDefaultsAndKeepBothHistoricalSnapshots()
    {
        await using var database = await SliceDatabase.CreateAsync();
        await SeedColorsAsync(database);
        var useCase = new StartArkNovaGameSessionUseCase(database);
        var first = Command(database);
        Guid firstId = await useCase.StartAsync(first, Token);
        await AssertDefaultsAsync(database, first);

        // The general group query is shared by other games and sees the new global order.
        var group = await new EditGroupUseCase(database).GetAsync(database.Group.Id, Token);
        Assert.Equal(first.Players.Select(player => player.PlayerId), group!.Players.Select(player => player.PlayerId));
        await using (var db = database.CreateDbContext())
        {
            var session = await db.GameSessions.Include(session => session.Players).SingleAsync(Token);
            session.BeginScoring(first.StartedAt.AddMinutes(10));
            session.Players.First().SetPlacement(1);
            session.Players.Last().SetPlacement(2);
            session.Complete(first.StartedAt.AddMinutes(11));
            await db.SaveChangesAsync(Token);
        }

        var preparation = await new ArkNovaPreparationQuery(database, TestGameContent.Create()).GetPreparationAsync(database.Group.Id, Token);
        Assert.NotNull(preparation!.LastGameSession);
        Assert.Equal(first.Players.Select(player => player.PlayerId), preparation.Players.Select(player => player.PlayerId));
        Assert.Equal(first.Players.Select(player => (PlayerColor?)player.Color), preparation.Players.Select(player => player.DefaultColor));
        var second = first with { StartedAt = first.StartedAt.AddHours(1), Players = [first.Players[1], first.Players[0]] };
        second = second with { Players = [second.Players[0] with { Color = PlayerColor.Yellow }, second.Players[1] with { Color = PlayerColor.Black }] };
        Guid secondId = await useCase.StartAsync(second, Token);
        await AssertDefaultsAsync(database, second);
        await using var verify = database.CreateDbContext();
        foreach (var pair in new[] { (firstId, first), (secondId, second) })
        {
            var players = await verify.GameSessionPlayers.AsNoTracking().Where(player => player.GameSessionId == pair.Item1)
                .OrderBy(player => player.Order).ToArrayAsync(Token);
            Assert.Equal(pair.Item2.Players.Select(player => player.PlayerId), players.Select(player => player.PlayerId));
            Assert.Equal(new[] { 1, 2 }, players.Select(player => player.Order));
            var details = await verify.ArkNovaGameSessionPlayers.AsNoTracking().Where(player => player.GameSessionId == pair.Item1).ToArrayAsync(Token);
            foreach (var expected in pair.Item2.Players)
            {
                var actual = Assert.Single(details, player => player.PlayerId == expected.PlayerId);
                Assert.Equal(expected.Color, actual.Color);
                Assert.Equal(expected.SelectedZooPlanNumber, actual.SelectedZooPlanNumber);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Start_CreatesMissingDefaultsAndUpdatesExistingOnes(bool partialDefaults)
    {
        await using var database = await SliceDatabase.CreateAsync();
        if (partialDefaults)
        {
            await using var db = database.CreateDbContext();
            db.ArkNovaGroupPlayers.Add(new(database.Group.Id, database.First.Id, PlayerColor.Blue));
            await db.SaveChangesAsync(Token);
        }
        var command = Command(database);
        await new StartArkNovaGameSessionUseCase(database).StartAsync(command, Token);
        await AssertDefaultsAsync(database, command);
    }

    [Theory]
    [InlineData("colors")]
    [InlineData("invalidColor")]
    [InlineData("selection")]
    [InlineData("membership")]
    [InlineData("duplicatePlayer")]
    [InlineData("count")]
    public async Task InvalidStart_KeepsDefaultsAndSessionInventory(string reason)
    {
        await using var database = await SliceDatabase.CreateAsync();
        await SeedColorsAsync(database);
        var command = Command(database);
        var player = command.Players[0];
        command = reason switch
        {
            "colors" => command with { Players = [player with { Color = PlayerColor.Red }, command.Players[1]] },
            "invalidColor" => command with { Players = [player with { Color = PlayerColor.Green }, command.Players[1]] },
            "selection" => command with { Players = [player with { SelectedZooPlanNumber = null }, command.Players[1]] },
            "membership" => command with { Players = [player with { PlayerId = Guid.NewGuid() }, command.Players[1]] },
            "duplicatePlayer" => command with { Players = [player, command.Players[1] with { PlayerId = player.PlayerId }] },
            _ => command with { Players = [] }
        };
        await Assert.ThrowsAnyAsync<Exception>(() => new StartArkNovaGameSessionUseCase(database).StartAsync(command, Token));
        await AssertUnchangedAsync(database);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistFailure_RollsBackAllWritesAndAllowsRetry(bool existingDefaults)
    {
        await using var database = await SliceDatabase.CreateAsync();
        if (existingDefaults)
            await SeedColorsAsync(database);
        await using (var db = database.CreateDbContext())
        {
            // Fail the child snapshot after defaults have been written and the session inserted.
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_start BEFORE INSERT ON ArkNovaGameSessionPlayers BEGIN SELECT RAISE(ABORT, 'test start failure'); END", Token);
        }
        var command = Command(database);
        await Assert.ThrowsAsync<DbUpdateException>(() => new StartArkNovaGameSessionUseCase(database).StartAsync(command, Token));
        if (existingDefaults)
            await AssertUnchangedAsync(database);
        await using (var db = database.CreateDbContext())
        {
            Assert.Empty(await db.GameSessions.ToArrayAsync(Token));
            Assert.Empty(await db.GameSessionPlayers.ToArrayAsync(Token));
            Assert.Empty(await db.ArkNovaGameSessionPlayers.ToArrayAsync(Token));
            Assert.Equal(new[] { database.First.Id, database.Second.Id }, await db.GroupPlayers.OrderBy(player => player.DefaultOrder).Select(player => player.PlayerId).ToArrayAsync(Token));
            if (!existingDefaults)
                Assert.Empty(await db.ArkNovaGroupPlayers.ToArrayAsync(Token));
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_start", Token);
        }
        await new StartArkNovaGameSessionUseCase(database).StartAsync(command, Token);
        await AssertDefaultsAsync(database, command);
    }

    private static async Task AssertUnchangedAsync(SliceDatabase database)
    {
        var initial = new StartArkNovaGameSessionCommand(database.Group.Id, DateTimeOffset.UtcNow,
            [new(database.First.Id, PlayerColor.Blue, 1), new(database.Second.Id, PlayerColor.Red, 3)]);
        await AssertDefaultsAsync(database, initial);
        await using var db = database.CreateDbContext();
        Assert.Empty(await db.GameSessions.ToArrayAsync(Token));
        Assert.Empty(await db.ArkNovaGameSessionPlayers.ToArrayAsync(Token));
    }

    private static async Task AssertDefaultsAsync(SliceDatabase database, StartArkNovaGameSessionCommand command)
    {
        await using var db = database.CreateDbContext();
        var members = await db.GroupPlayers.Where(player => player.GroupId == database.Group.Id).OrderBy(player => player.DefaultOrder).ToArrayAsync(Token);
        Assert.Equal(command.Players.Select(player => player.PlayerId), members.Select(player => player.PlayerId));
        Assert.Equal(Enumerable.Range(1, command.Players.Count), members.Select(player => player.DefaultOrder));
        var colors = await db.ArkNovaGroupPlayers.Where(player => player.GroupId == database.Group.Id).ToArrayAsync(Token);
        Assert.Equal(command.Players.Count, colors.Length);
        foreach (var player in command.Players)
            Assert.Equal(player.Color, Assert.Single(colors, actual => actual.PlayerId == player.PlayerId).DefaultColor);
    }
}

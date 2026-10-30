using BoardgameTracker.App.Persistence.SQLite;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Application.Games.ArkNova.Persistence;
using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.Groups;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Application.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Groups.Colors;

public sealed class GroupColorPersistenceTests
{
    [Fact]
    public async Task UpdateGroupAsync_RejectsDefaultColorForUnknownPlayer()
    {
        await using var fixture = await Fixture.CreateAsync();
        var colors = fixture.Colors();
        colors.Remove(fixture.First.Id);
        colors.Add(Guid.NewGuid(), PlayerColor.Blue);
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.UseCase.UpdateGroupAsync(
            fixture.Command(colors), TestContext.Current.CancellationToken));
        await fixture.AssertOriginalAsync();
    }

    [Fact]
    public async Task UpdateGroupAsync_RejectsDuplicateDefaultColors()
    {
        await using var fixture = await Fixture.CreateAsync();
        var colors = fixture.Colors();
        colors[fixture.Second.Id] = PlayerColor.Blue;
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UseCase.UpdateGroupAsync(
            fixture.Command(colors), TestContext.Current.CancellationToken));
        await fixture.AssertOriginalAsync();
    }

    [Fact]
    public async Task UpdateGroupAsync_RejectsUndefinedDefaultColor()
    {
        await using var fixture = await Fixture.CreateAsync();
        var colors = fixture.Colors();
        colors[fixture.First.Id] = (PlayerColor)999;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.UseCase.UpdateGroupAsync(
            fixture.Command(colors), TestContext.Current.CancellationToken));
        await fixture.AssertOriginalAsync();
    }

    [Fact]
    public async Task UpdateGroupAsync_SwapsOrderAndColorsWithCommonMetadataInOneEdit()
    {
        await using var fixture = await Fixture.CreateAsync();
        var colors = fixture.Colors();
        colors[fixture.First.Id] = PlayerColor.Red;
        colors[fixture.Second.Id] = PlayerColor.Blue;
        await fixture.UseCase.UpdateGroupAsync(fixture.Command(colors), TestContext.Current.CancellationToken);
        var result = await fixture.UseCase.GetAsync(fixture.Group.Id, ArkNovaGame.Key, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("Neu", result.Name);
        Assert.True(result.IsArchived);
        Assert.False(result.IsTemporary);
        Assert.Equal(fixture.Second.Id, result.Players[0].PlayerId);
        Assert.Equal(PlayerColor.Red, result.Players.Single(player => player.PlayerId == fixture.First.Id).DefaultColor);
        Assert.Equal(PlayerColor.Blue, result.Players.Single(player => player.PlayerId == fixture.Second.Id).DefaultColor);
    }

    [Fact]
    public async Task CommonEdit_DoesNotReadOrReplaceGameSpecificDefaults()
    {
        await using var fixture = await Fixture.CreateAsync();
        var common = fixture.UseCase;
        await common.UpdateGroupAsync(fixture.Command() with { ColorDefaults = null }, TestContext.Current.CancellationToken);
        await using var db = fixture.CreateDbContext();
        var colors = await new ArkNovaGroupColorPersistence().ReadAsync(db, fixture.Group.Id, TestContext.Current.CancellationToken);
        Assert.Equal(PlayerColor.Blue, colors[fixture.First.Id]);
        Assert.Equal(PlayerColor.Red, colors[fixture.Second.Id]);
        Assert.Equal("Neu", (await common.GetAsync(fixture.Group.Id, cancellationToken: TestContext.Current.CancellationToken))!.Name);
    }

    [Fact]
    public async Task UpdateGroupAsync_WhenColorInsertFails_RollsBackCommonChangesAndDefaults()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var db = fixture.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER RejectColorInsert BEFORE INSERT ON ArkNovaGroupPlayers
                BEGIN SELECT RAISE(ABORT, 'Simulated color write failure'); END;
                """, TestContext.Current.CancellationToken);
        }
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.UseCase.UpdateGroupAsync(
            fixture.Command(fixture.Colors()), TestContext.Current.CancellationToken));
        await fixture.AssertOriginalAsync();
    }

    [Fact]
    public async Task GetAsync_GroupWithoutGameDefaults_ReturnsUnassignedColors()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var db = fixture.CreateDbContext())
            await db.ArkNovaGroupPlayers.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        var result = await fixture.UseCase.GetAsync(fixture.Group.Id, ArkNovaGame.Key, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(2, result.Players.Count);
        Assert.All(result.Players, player => Assert.Null(player.DefaultColor));
    }

    [Fact]
    public async Task CreateAsync_SavesExistingAndNewPlayersWithTheirSelectedColors()
    {
        await using var fixture = await Fixture.CreateAsync();
        var create = new CreateGroupUseCase(fixture, fixture.Catalog, [new ArkNovaGroupColorPersistence()]);
        var id = await create.CreateGroupAsync(new("Neu", false,
            [new(null, "Chris", false, PlayerColor.Red), new(fixture.First.Id, fixture.First.Name, false, PlayerColor.Blue)],
            ArkNovaGame.Key), TestContext.Current.CancellationToken);
        var result = await fixture.UseCase.GetAsync(id, ArkNovaGame.Key, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("Chris", result.Players[0].Name);
        Assert.Equal(PlayerColor.Red, result.Players[0].DefaultColor);
        Assert.Equal(fixture.First.Id, result.Players[1].PlayerId);
        Assert.Equal(PlayerColor.Blue, result.Players[1].DefaultColor);
    }

    [Fact]
    public async Task CreateAsync_ColorWriteFailure_DoesNotLeaveGroupOrNewPlayer()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var db = fixture.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER RejectNewColor BEFORE INSERT ON ArkNovaGroupPlayers
                BEGIN SELECT RAISE(ABORT, 'Simulated color write failure'); END;
                """, TestContext.Current.CancellationToken);
        var create = new CreateGroupUseCase(fixture, fixture.Catalog, [new ArkNovaGroupColorPersistence()]);
        await Assert.ThrowsAsync<DbUpdateException>(() => create.CreateGroupAsync(new("Neu", false,
            [new(null, "Chris", false, PlayerColor.Red)], ArkNovaGame.Key), TestContext.Current.CancellationToken));
        await using var verify = fixture.CreateDbContext();
        Assert.Equal(1, await verify.Groups.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await verify.Players.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ColorlessGame_HasNoChoices_RejectsColorsAndPreservesOtherGameDefaults()
    {
        await using var fixture = await Fixture.CreateAsync();
        const string colorlessGame = "without-colors";
        Assert.Empty(fixture.Catalog.Get(colorlessGame).AllowedColors);
        var result = await fixture.UseCase.GetAsync(fixture.Group.Id, colorlessGame, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.All(result.Players, player => Assert.Null(player.DefaultColor));
        var command = fixture.Command() with
        {
            ColorDefaults = new(colorlessGame, new Dictionary<Guid, PlayerColor?>
            {
                [fixture.First.Id] = null,
                [fixture.Second.Id] = null
            })
        };
        await fixture.UseCase.UpdateGroupAsync(command, TestContext.Current.CancellationToken);
        var arkNova = await fixture.UseCase.GetAsync(fixture.Group.Id, ArkNovaGame.Key, TestContext.Current.CancellationToken);
        Assert.Equal(PlayerColor.Blue, arkNova!.Players.Single(player => player.PlayerId == fixture.First.Id).DefaultColor);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.UseCase.UpdateGroupAsync(
            command with { ColorDefaults = new(colorlessGame, fixture.Colors()) }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_RejectsGlobalColorOutsideCurrentGameRules()
    {
        await using var fixture = await Fixture.CreateAsync();
        var create = new CreateGroupUseCase(fixture, fixture.Catalog, [new ArkNovaGroupColorPersistence()]);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => create.CreateGroupAsync(new("Neu", false,
            [new(null, "Chris", false, PlayerColor.Green)], ArkNovaGame.Key), TestContext.Current.CancellationToken));
        await using var db = fixture.CreateDbContext();
        Assert.Equal(2, await db.Players.CountAsync(TestContext.Current.CancellationToken));
    }

    private sealed class Fixture : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public Player First { get; } = new("Anna");
        public Player Second { get; } = new("Ben");
        public Group Group { get; private set; } = null!;
        public GamePlayerColorCatalog Catalog => new([ArkNovaPlayerColors.Rules]);
        public EditGroupUseCase UseCase => new(this, Catalog, [new ArkNovaGroupColorPersistence()]);

        public BoardgameTrackerDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<BoardgameTrackerDbContext>();
            SqliteServiceCollectionExtensions.Configure(options, _connection.ConnectionString);
            options.UseTestSqlite(_connection);
            return new(options.Options);
        }

        public static async Task<Fixture> CreateAsync()
        {
            var fixture = new Fixture();
            await fixture._connection.OpenAsync(TestContext.Current.CancellationToken);
            fixture.Group = new("Alt", true, [fixture.First.Id, fixture.Second.Id]);
            await using var db = fixture.CreateDbContext();
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            db.AddRange(fixture.First, fixture.Second, fixture.Group,
                new ArkNovaGroupPlayer(fixture.Group.Id, fixture.First.Id, PlayerColor.Blue),
                new ArkNovaGroupPlayer(fixture.Group.Id, fixture.Second.Id, PlayerColor.Red));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            return fixture;
        }

        public EditGroupCommand Command(IReadOnlyDictionary<Guid, PlayerColor?>? colors = null) => new(Group.Id, "Neu", true, true, [Second.Id, First.Id], new(ArkNovaGame.Key, colors ?? Colors()));
        public Dictionary<Guid, PlayerColor?> Colors() => new()
        {
            [First.Id] = PlayerColor.Blue,
            [Second.Id] = PlayerColor.Red
        };

        public async Task AssertOriginalAsync()
        {
            var result = await UseCase.GetAsync(Group.Id, ArkNovaGame.Key, TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.Equal("Alt", result.Name);
            Assert.True(result.IsTemporary);
            Assert.False(result.IsArchived);
            Assert.Equal(First.Id, result.Players[0].PlayerId);
            Assert.Equal(PlayerColor.Blue, result.Players.Single(player => player.PlayerId == First.Id).DefaultColor);
            Assert.Equal(PlayerColor.Red, result.Players.Single(player => player.PlayerId == Second.Id).DefaultColor);
        }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}

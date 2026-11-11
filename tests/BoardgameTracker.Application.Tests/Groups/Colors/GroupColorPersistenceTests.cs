using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Groups.Colors;

public sealed class GroupColorPersistenceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Module_RejectsDefaultColorForUnknownPlayer()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        Dictionary<Guid, PlayerColor?> colors = fixture.Colors();
        colors.Remove(fixture.First.Id);
        colors.Add(Guid.NewGuid(), PlayerColor.Blue);
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.ReplaceColorsAsync(colors));
        await fixture.AssertOriginalAsync();
    }

    [Fact]
    public async Task Module_RejectsDuplicateDefaultColors()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        Dictionary<Guid, PlayerColor?> colors = fixture.Colors();
        colors[fixture.Second.Id] = PlayerColor.Blue;
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.ReplaceColorsAsync(colors));
        await fixture.AssertOriginalAsync();
    }

    [Fact]
    public async Task Module_RejectsUndefinedDefaultColor()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        Dictionary<Guid, PlayerColor?> colors = fixture.Colors();
        colors[fixture.First.Id] = (PlayerColor)999;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.ReplaceColorsAsync(colors));
        await fixture.AssertOriginalAsync();
    }

    [Fact]
    public async Task CommonEdit_SwapsOrderAndMetadataWithoutChangingDefaults()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        await fixture.UseCase.UpdateGroupAsync(fixture.Command(), Token);
        EditGroupDetails result = (await fixture.UseCase.GetAsync(fixture.Group.Id, Token))!;
        Assert.Equal("Neu", result.Name);
        Assert.True(result.IsArchived);
        Assert.Equal(fixture.Second.Id, result.Players[0].PlayerId);
        await fixture.AssertColorsAsync();
    }

    [Fact]
    public async Task CommonEdit_DoesNotReadOrReplaceGameSpecificDefaults()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        await using (BoardgameTrackerDbContext db = fixture.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectColorInsert BEFORE INSERT ON GroupGamePlayerColors BEGIN SELECT RAISE(ABORT, 'unexpected color write'); END;", Token);
        await fixture.UseCase.UpdateGroupAsync(fixture.Command(), Token);
        await fixture.AssertColorsAsync();
        Assert.Equal("Neu", (await fixture.UseCase.GetAsync(fixture.Group.Id, Token))!.Name);
    }

    [Fact]
    public async Task Module_WhenColorInsertFails_RollsBackDefaults()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        await using (BoardgameTrackerDbContext db = fixture.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectColorInsert BEFORE INSERT ON GroupGamePlayerColors BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.ReplaceColorsAsync(fixture.Colors()));
        await fixture.AssertOriginalAsync();
    }

    [Fact]
    public async Task GetAsync_GroupWithoutGameDefaults_ReturnsMembersWithoutCreatingDefaults()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        await using (BoardgameTrackerDbContext db = fixture.CreateDbContext())
            await db.GroupGamePlayerColors.ExecuteDeleteAsync(Token);
        EditGroupDetails result = (await fixture.UseCase.GetAsync(fixture.Group.Id, Token))!;
        Assert.Equal(2, result.Players.Count);
        await using BoardgameTrackerDbContext verify = fixture.CreateDbContext();
        Assert.Empty(await verify.GroupGamePlayerColors.ToArrayAsync(Token));
    }

    [Fact]
    public async Task CreateAsync_SavesExistingAndNewPlayersWithoutWritingDefaults()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        Guid id = await new CreateGroupUseCase(fixture).CreateGroupAsync(new("Neu",
            [new(null, "Chris", false), new(fixture.First.Id, fixture.First.Name, false)], []), Token);
        EditGroupDetails result = (await fixture.UseCase.GetAsync(id, Token))!;
        Assert.Equal("Chris", result.Players[0].Name);
        Assert.Equal(fixture.First.Id, result.Players[1].PlayerId);
        await fixture.AssertColorsAsync();
        await using BoardgameTrackerDbContext verify = fixture.CreateDbContext();
        Assert.Empty(await verify.GroupGamePlayerColors.Where(player => player.GroupId == id).ToArrayAsync(Token));
    }

    [Fact]
    public async Task CreateAsync_MembershipWriteFailure_DoesNotLeaveGroupOrNewPlayer()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        await using (BoardgameTrackerDbContext db = fixture.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectMembership BEFORE INSERT ON GroupPlayers BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        await Assert.ThrowsAsync<DbUpdateException>(() => new CreateGroupUseCase(fixture).CreateGroupAsync(
            new("Neu", [new(null, "Chris", false)], []), Token));
        await using BoardgameTrackerDbContext verify = fixture.CreateDbContext();
        Assert.Equal(1, await verify.Groups.CountAsync(Token));
        Assert.Equal(2, await verify.Players.CountAsync(Token));
        await fixture.AssertColorsAsync();
    }

    [Fact]
    public async Task ColorlessGame_HasNoChoicesAndCommonEditsPreserveDefaults()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        GamePlayerColorCatalog catalog = new([ArkNovaPlayerColors.Rules]);
        Assert.Empty(catalog.Get("without-colors").AllowedColors);
        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.Get("without-colors").Validate([PlayerColor.Blue]));
        await fixture.UseCase.UpdateGroupAsync(fixture.Command(), Token);
        await fixture.AssertColorsAsync();
    }

    [Fact]
    public async Task Module_RejectsGlobalColorOutsideCurrentGameRules()
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        Dictionary<Guid, PlayerColor?> colors = fixture.Colors();
        colors[fixture.First.Id] = PlayerColor.Green;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.ReplaceColorsAsync(colors));
        await fixture.AssertOriginalAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReuseOrReactivate_PreservesExistingDefaults(bool archived)
    {
        await using Fixture fixture = await Fixture.CreateAsync();
        await using (BoardgameTrackerDbContext db = fixture.CreateDbContext())
        {
            (await db.Groups.SingleAsync(Token)).SetArchived(archived);
            await db.SaveChangesAsync(Token);
        }
        await new ReuseGroupUseCase(fixture).ReuseGroupAsync(new(fixture.Group.Id,
            [fixture.Second.Id, fixture.First.Id], [], archived ? GroupReuseAction.Reactivate : GroupReuseAction.UseExisting), Token);
        await fixture.AssertOriginalAsync();
    }

    private sealed class Fixture : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public Player First { get; } = new("Anna");
        public Player Second { get; } = new("Ben");
        public Group Group { get; private set; } = null!;
        public BoardgameTracker.Core.Games.Game Game { get; } = new(ArkNovaGame.Key, ArkNovaGame.Name, ArkNovaGame.BadgeText, 1, 4);
        public EditGroupUseCase UseCase => new(this);
        public BoardgameTrackerDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().UseTestSqlite(_connection).Options);
        public static async Task<Fixture> CreateAsync()
        {
            Fixture fixture = new();
            await fixture._connection.OpenAsync(Token);
            fixture.Group = new("Alt", [fixture.First.Id, fixture.Second.Id]);
            await using BoardgameTrackerDbContext db = fixture.CreateDbContext();
            await db.Database.EnsureCreatedAsync(Token);
            db.AddRange(fixture.First, fixture.Second, fixture.Group, fixture.Game,
                new GroupGamePlayerColor(fixture.Group.Id, fixture.Game.Id, fixture.First.Id, PlayerColor.Blue),
                new GroupGamePlayerColor(fixture.Group.Id, fixture.Game.Id, fixture.Second.Id, PlayerColor.Red));
            await db.SaveChangesAsync(Token);
            return fixture;
        }
        public EditGroupCommand Command() => new(Group.Id, "Neu", true, [Second.Id, First.Id], []);
        public Dictionary<Guid, PlayerColor?> Colors() => new() { [First.Id] = PlayerColor.Blue, [Second.Id] = PlayerColor.Red };
        public async Task ReplaceColorsAsync(IReadOnlyDictionary<Guid, PlayerColor?> colors)
        {
            await using BoardgameTrackerDbContext db = CreateDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync(Token);
            ArkNovaPlayerColors.Rules.Validate(colors.Values);
            await db.GroupGamePlayerColors.Where(color => color.GroupId == Group.Id && color.GameId == Game.Id).ExecuteDeleteAsync(Token);
            db.GroupGamePlayerColors.AddRange(colors.Where(pair => pair.Value.HasValue)
                .Select(pair => new GroupGamePlayerColor(Group.Id, Game.Id, pair.Key, pair.Value!.Value)));
            await db.SaveChangesAsync(Token);
            await transaction.CommitAsync(Token);
        }
        public async Task AssertColorsAsync()
        {
            await using BoardgameTrackerDbContext db = CreateDbContext();
            var colors = await db.GroupGamePlayerColors.Where(color => color.GroupId == Group.Id && color.GameId == Game.Id)
                .ToDictionaryAsync(color => color.PlayerId, color => color.DefaultColor, Token);
            Assert.Equal(2, colors.Count);
            Assert.Equal(PlayerColor.Blue, colors[First.Id]);
            Assert.Equal(PlayerColor.Red, colors[Second.Id]);
        }
        public async Task AssertOriginalAsync()
        {
            EditGroupDetails result = (await UseCase.GetAsync(Group.Id, Token))!;
            Assert.Equal("Alt", result.Name);
            Assert.False(result.IsArchived);
            Assert.Equal(First.Id, result.Players[0].PlayerId);
            await AssertColorsAsync();
        }
        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}

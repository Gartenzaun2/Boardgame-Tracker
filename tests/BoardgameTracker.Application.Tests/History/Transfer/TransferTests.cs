using System.Text;
using System.Text.Json;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Application.Games.ArkNova.History.Transfer;
using BoardgameTracker.Core.Names;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite.History;
using BoardgameTracker.Infrastructure.Games.ArkNova.History.Transfer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.History.Transfer;

public sealed class TransferTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static BgtFormat Format => new([new ArkNovaTransferSchema()]);
    private static JsonElement Empty => JsonSerializer.SerializeToElement(new { });
    private static BgtFile File(BgtPlayer[] players, BgtGroup[] groups, BgtGameSession[] gameSessions) => new(1, "test", DateTimeOffset.UtcNow,
        [new(GameId, "ark-nova", 1, JsonSerializer.SerializeToElement(new ArkNovaTransferReferences([]), BgtFormat.JsonOptions))], players, groups, gameSessions);
    private static readonly Guid GameId = Guid.NewGuid();
    private static BgtPlayer Player(string name) => new(Guid.NewGuid(), name, false, false, DateTimeOffset.UtcNow);
    private static BgtGroup Group(string name, params BgtPlayer[] players) => new(Guid.NewGuid(), name, false, false,
        players.Select((player, index) => new BgtGroupPlayer(player.Id, index + 1)).ToArray());
    private static BgtGameSession GameSession(BgtGroup group) => new(Guid.NewGuid(), GameId, group.Id, DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow,
        group.GroupPlayers.Select(player => new BgtGameSessionPlayer(player.PlayerId, player.DefaultOrder, 1)).ToArray(), Empty);

    [Theory]
    [InlineData("Anna", " anna ", true)]
    [InlineData("Anna B.", "Anna B", false)]
    [InlineData("Spieleabend", "Spiele Abend", false)]
    [InlineData("ÄNNA", "änna", true)]
    public void NamesUseExactOrdinalTrimSemantics(string left, string right, bool expected) => Assert.Equal(expected, EntityName.Equals(left, right));

    [Fact]
    public void IdentityAndNameMatchingKeepLocalIdsAndSkipUnusedDependencies()
    {
        BgtPlayer anna = Player("Anna");
        BgtPlayer ben = Player("Ben");
        BgtGroup group = Group("Montag", anna, ben);
        BgtGameSession gameSession = GameSession(group);
        Guid benLocal = Guid.NewGuid();
        TransferSnapshot local = new([new(anna.Id, "Renamed"), new(benLocal, " ben ")],
            [new(Guid.NewGuid(), " MONTAG ", [anna.Id, benLocal])], []);
        ImportPlan plan = ImportPlanner.Analyze(File([anna, ben], [group], [gameSession]), local);
        Assert.True(plan.CanCommit);
        Assert.All(plan.Players, player => Assert.False(player.IsNew));
        Assert.Equal(benLocal, plan.Players.Single(player => player.Imported.Id == ben.Id).LocalId);
        Assert.False(Assert.Single(plan.Groups).IsNew);
        ImportPlan repeated = ImportPlanner.Analyze(plan.Source, local with { GameSessionIds = [gameSession.Id] });
        Assert.Empty(repeated.Players);
        Assert.Empty(repeated.Groups);
        Assert.Empty(repeated.GameSessions);
        Assert.Equal(1, repeated.Skipped);
    }
    [Fact]
    public void ResolvingOneGroupRecalculatesOthersAndSplitKeepsGlobalMapping()
    {
        BgtPlayer anna = Player("Anna");
        BgtPlayer ben = Player("Ben");
        BgtPlayer tom = Player("Tom");
        BgtGroup monday = Group("Montag", anna, tom);
        BgtGroup friday = Group("Freitag", ben, tom);
        Guid thomas = Guid.NewGuid();
        Guid tobias = Guid.NewGuid();
        TransferSnapshot local = new([new(anna.Id, anna.Name), new(ben.Id, ben.Name), new(thomas, "Thomas"), new(tobias, "Tobias")],
            [new(Guid.NewGuid(), monday.Name, [anna.Id, thomas]), new(Guid.NewGuid(), friday.Name, [ben.Id, tobias])], []);
        BgtFile file = File([anna, ben, tom], [monday, friday], [GameSession(monday), GameSession(friday)]);
        Assert.Equal(2, ImportPlanner.Analyze(file, local).Conflicts.Length);
        GroupResolution resolve = new(monday.Id, false, new Dictionary<Guid, Guid> { [tom.Id] = thomas });
        ImportPlan plan = ImportPlanner.Analyze(file, local, [resolve]);
        Assert.True(plan.CanCommit);
        Assert.Equal(thomas, plan.Players.Single(player => player.Imported.Id == tom.Id).LocalId);
        Assert.Equal("Freitag Import", plan.Groups.Single(group => group.Imported.Id == friday.Id).Name);
        Assert.Throws<InvalidDataException>(() => ImportPlanner.Analyze(file, local, [resolve,
            new(friday.Id, false, new Dictionary<Guid, Guid> { [tom.Id] = tobias })]));
    }
    [Fact]
    public void SplitNamesAreDeterministicAndDirectGroupIdentityCannotChangeComposition()
    {
        BgtPlayer player = Player("Player");
        BgtGroup group = Group("Montag", player);
        TransferSnapshot local = new([], [new(Guid.NewGuid(), "Montag", [Guid.NewGuid(), Guid.NewGuid()]), new(Guid.NewGuid(), "Montag Import", [])], []);
        BgtFile file = File([player], [group], [GameSession(group)]);
        Assert.Equal("Montag Import 2", Assert.Single(ImportPlanner.Analyze(file, local).Groups).Name);
        Assert.Throws<InvalidDataException>(() => ImportPlanner.Analyze(file, local with { Groups = [new(group.Id, group.Name, [Guid.NewGuid(), Guid.NewGuid()])] }));
    }
    [Fact]
    public void TwoImportedPlayersCannotMapToOneLocalPlayer()
    {
        BgtPlayer first = Player("First");
        BgtPlayer second = Player("Second");
        BgtGroup group = Group("Group", first, second);
        Assert.Throws<InvalidDataException>(() => ImportPlanner.Analyze(File([first, second], [group], [GameSession(group)]),
            new([new(first.Id, second.Name)], [], [])));
    }
    [Fact]
    public async Task FormatRejectsVersionsEncodingPropertiesAndLimits()
    {
        foreach (string json in new[] { "{\"formatVersion\":2}", "{\"formatVersion\":0}", "{\"formatVersion\":1,\"formatVersion\":1}" })
        {
            using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
            await Assert.ThrowsAnyAsync<Exception>(() => Format.ReadAsync(stream, Token));
        }
        using MemoryStream invalidUtf8 = new([0xff, 0xfe]);
        await Assert.ThrowsAsync<DecoderFallbackException>(() => Format.ReadAsync(invalidUtf8, Token));
        using MemoryStream oversized = new(new byte[BgtFormat.MaximumBytes + 1]);
        await Assert.ThrowsAsync<InvalidDataException>(() => Format.ReadAsync(oversized, Token));
        using MemoryStream deep = new(Encoding.UTF8.GetBytes(new string('[', 40) + "0" + new string(']', 40)));
        await Assert.ThrowsAnyAsync<JsonException>(() => Format.ReadAsync(deep, Token));
        BgtFormat.CheckIds([Guid.NewGuid()]);
        Assert.Throws<InvalidDataException>(() => BgtFormat.CheckIds([Guid.Empty]));
        Assert.Throws<InvalidDataException>(() => BgtFormat.CheckIds(Enumerable.Range(0, BgtFormat.MaximumItems + 1).Select(_ => Guid.NewGuid())));
        using MemoryStream unknown = new(Encoding.UTF8.GetBytes("{\"formatVersion\":1,\"appVersion\":\"x\",\"exportedAtUtc\":\"2026-10-30T12:00:00Z\",\"games\":[],\"players\":[],\"groups\":[],\"gameSessions\":[],\"future\":1}"));
        await Assert.ThrowsAsync<JsonException>(() => Format.ReadAsync(unknown, Token));
    }
    [Fact]
    public async Task SqliteEnforcesNamesAndUuidVersions()
    {
        await using Database db = await Database.CreateAsync();
        await using BoardgameTrackerDbContext context = db.CreateDbContext();
        Player player = new("Änna");
        Group group = new("Group", false, [player.Id]);
        Assert.Equal(7, player.Id.Version);
        Assert.Equal(7, group.Id.Version);
        Assert.Equal(7, new GameSession(GameId, group.Id, DateTimeOffset.UtcNow, [player.Id]).Id.Version);
        context.AddRange(player, group);
        await context.SaveChangesAsync(Token);
        context.Players.Add(new(" änNA "));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Token));
        context.ChangeTracker.Clear();
        context.Groups.Add(new(" group ", false, [player.Id]));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Token));
    }
    [Fact]
    public async Task RoundtripPreservesIdsValuesAndLocalStateAndRepeatedImportDoesNothing()
    {
        await using Database source = await Database.CreateAsync();
        Guid id = await source.SeedAsync();
        BgtFile exported = await source.Store.ExportAsync([id], "test", Token);
        Format.Validate(exported);
        Assert.Single(exported.GameSessions);
        Assert.Equal(2, exported.Players.Length);
        Assert.Single(exported.Groups);
        await using Database target = await Database.CreateAsync();
        HistoryTransferUseCase useCase = new(target.Store, Format);
        using MemoryStream stream = new(JsonSerializer.SerializeToUtf8Bytes(exported, BgtFormat.JsonOptions));
        ImportPlan plan = await useCase.AnalyzeAsync(stream, Token);
        Assert.Equal(0, await target.CountAsync());
        await useCase.PreviewAsync(plan, Token);
        Assert.Equal(0, await target.CountAsync());
        await useCase.CommitAsync(plan, Token);
        BgtFile again = await target.Store.ExportAsync([id], "test", Token);
        Assert.Equal(JsonSerializer.Serialize(exported.Players.OrderBy(player => player.Id)), JsonSerializer.Serialize(again.Players.OrderBy(player => player.Id)));
        Assert.Equal(exported.GameSessions[0].Data.GetRawText(), again.GameSessions[0].Data.GetRawText());
        ImportPlan repeated = ImportPlanner.Analyze(exported, await target.Store.ReadSnapshotAsync(Token));
        Assert.Empty(repeated.Players);
        await useCase.CommitAsync(repeated, Token);
        Assert.Equal(1, await target.CountAsync());
    }
    [Fact]
    public async Task CommitFailureRollsBackEntireImport()
    {
        await using Database source = await Database.CreateAsync();
        Guid id = await source.SeedAsync();
        BgtFile exported = await source.Store.ExportAsync([id], "test", Token);
        await using Database target = await Database.CreateAsync();
        await using (BoardgameTrackerDbContext context = target.CreateDbContext())
            await context.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectGameSession BEFORE INSERT ON GameSessions BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        ImportPlan plan = ImportPlanner.Analyze(exported, await target.Store.ReadSnapshotAsync(Token));
        await Assert.ThrowsAsync<DbUpdateException>(() => target.Store.CommitAsync(plan, Token));
        await using BoardgameTrackerDbContext verify = target.CreateDbContext();
        Assert.Empty(await verify.Players.ToArrayAsync(Token));
        Assert.Empty(await verify.Groups.ToArrayAsync(Token));
        Assert.Empty(await verify.GameSessions.ToArrayAsync(Token));
    }
    [Fact]
    public async Task ExportUsesExactlySelected23CompletedGameSessionsAndDependencies()
    {
        await using Database db = await Database.CreateAsync();
        List<Guid> selected = [];
        for (int index = 0; index < 23; index++)
            selected.Add(await db.SeedAsync(index.ToString()));
        Guid excluded = await db.SeedAsync("excluded");
        BgtFile file = await db.Store.ExportAsync(selected, "test", Token);
        Assert.Equal(23, file.GameSessions.Length);
        Assert.True(selected.ToHashSet().SetEquals(file.GameSessions.Select(gameSession => gameSession.Id)));
        Assert.DoesNotContain(file.GameSessions, gameSession => gameSession.Id == excluded);
        Assert.Equal(23, file.Groups.Length);
        Assert.Equal(46, file.Players.Length);
        Assert.DoesNotContain(file.Players, player => player.Name.Contains("excluded", StringComparison.Ordinal));
        BgtFile empty = await db.Store.ExportAsync([], "test", Token);
        Assert.Empty(empty.GameSessions);
        Assert.Empty(empty.Players);
        Assert.Empty(empty.Games);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SpecialCardMatchingKeepsLocalStateAndRejectsAmbiguousNames(bool sameId)
    {
        await using Database source = await Database.CreateAsync();
        Guid id = await source.SeedAsync();
        Guid foreignCard = Guid.NewGuid();
        await using (BoardgameTrackerDbContext db = source.CreateDbContext())
        {
            db.ArkNovaSpecialCards.Add(new(foreignCard, "Card", ArkNovaCardSymbol.Africa));
            Guid player = await db.GameSessionPlayers.Select(player => player.PlayerId).FirstAsync(Token);
            db.ArkNovaGameSessionSpecialCardAssignments.Add(new(id, player, foreignCard));
            await db.SaveChangesAsync(Token);
        }
        BgtFile file = await source.Store.ExportAsync([id], "test", Token);
        Format.Validate(file);
        await using Database target = await Database.CreateAsync();
        Guid localCard = sameId ? foreignCard : Guid.NewGuid();
        await using (BoardgameTrackerDbContext db = target.CreateDbContext())
        {
            ArkNovaSpecialCard card = new(localCard, " CARD ");
            card.SetArchived(true);
            db.ArkNovaSpecialCards.Add(card);
            await db.SaveChangesAsync(Token);
        }
        ImportPlan plan = ImportPlanner.Analyze(file, await target.Store.ReadSnapshotAsync(Token));
        TransferPreview preview = await target.Store.PreviewAsync(plan, Token);
        Assert.Equal(1, preview.ReferencesExisting);
        await target.Store.CommitAsync(plan, Token);
        await using (BoardgameTrackerDbContext db = target.CreateDbContext())
        {
            Assert.True((await db.ArkNovaSpecialCards.SingleAsync(Token)).IsArchived);
            Assert.Equal(localCard, (await db.ArkNovaGameSessionSpecialCardAssignments.SingleAsync(Token)).SpecialCardId);
        }
        await using Database ambiguous = await Database.CreateAsync();
        await using (BoardgameTrackerDbContext db = ambiguous.CreateDbContext())
        {
            db.ArkNovaSpecialCards.AddRange(new ArkNovaSpecialCard(Guid.NewGuid(), "Card"), new ArkNovaSpecialCard(Guid.NewGuid(), "card"));
            await db.SaveChangesAsync(Token);
        }
        ImportPlan invalid = ImportPlanner.Analyze(file, await ambiguous.Store.ReadSnapshotAsync(Token));
        await Assert.ThrowsAsync<InvalidDataException>(() => ambiguous.Store.PreviewAsync(invalid, Token));
        Assert.Equal(0, await ambiguous.CountAsync());
    }
    [Fact]
    public void ValidationRejectsUnknownGamesVersionsDuplicateNamesAndMissingReferences()
    {
        BgtPlayer player = Player("Anna");
        BgtGroup group = Group("Group", player);
        BgtFile file = File([player], [group], []);
        Assert.Throws<InvalidDataException>(() => Format.Validate(file with { Games = [file.Games[0] with { Key = "unknown" }] }));
        Assert.Throws<InvalidDataException>(() => Format.Validate(file with { Games = [file.Games[0] with { SchemaVersion = 2 }] }));
        Assert.Throws<InvalidDataException>(() => Format.Validate(file with { Players = [player, Player(" anna ")] }));
        Assert.Throws<InvalidDataException>(() => Format.Validate(file with { Groups = [group, group with { Id = Guid.NewGuid() }] }));
        Assert.Throws<InvalidDataException>(() => Format.Validate(file with { Players = [] }));
        Assert.Throws<InvalidDataException>(() => Format.Validate(file with { GameSessions = [GameSession(group) with { GroupId = Guid.NewGuid() }] }));
    }
    [Fact]
    public async Task MatchedPlayersKeepTheirLocalNamesGuestArchiveAndCreationValues()
    {
        await using Database source = await Database.CreateAsync();
        Guid id = await source.SeedAsync();
        BgtFile file = await source.Store.ExportAsync([id], "test", Token);
        BgtPlayer imported = file.Players.Single(player => player.IsGuest);
        await using Database target = await Database.CreateAsync();
        DateTimeOffset creation = DateTimeOffset.UtcNow.AddYears(-1);
        await using (BoardgameTrackerDbContext db = target.CreateDbContext())
        {
            db.Players.Add(new("Local renamed", false, imported.Id, creation));
            await db.SaveChangesAsync(Token);
        }
        ImportPlan plan = ImportPlanner.Analyze(file, await target.Store.ReadSnapshotAsync(Token));
        await target.Store.CommitAsync(plan, Token);
        await using BoardgameTrackerDbContext verify = target.CreateDbContext();
        Player local = await verify.Players.SingleAsync(player => player.Id == imported.Id, Token);
        Assert.Equal("Local renamed", local.Name);
        Assert.False(local.IsGuest);
        Assert.False(local.IsArchived);
        Assert.Equal(creation, local.CreatedAt);
    }
    [Fact]
    public async Task ChangesAfterAnalyzeRequireNewAnalysisWithoutWrites()
    {
        await using Database source = await Database.CreateAsync();
        Guid id = await source.SeedAsync();
        BgtFile file = await source.Store.ExportAsync([id], "test", Token);
        await using Database target = await Database.CreateAsync();
        ImportPlan plan = ImportPlanner.Analyze(file, await target.Store.ReadSnapshotAsync(Token));
        await using (BoardgameTrackerDbContext db = target.CreateDbContext())
        {
            db.ArkNovaSpecialCards.Add(new(Guid.NewGuid(), "New local card"));
            await db.SaveChangesAsync(Token);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => target.Store.CommitAsync(plan, Token));
        await using BoardgameTrackerDbContext verify = target.CreateDbContext();
        Assert.Empty(await verify.Players.ToArrayAsync(Token));
    }
    private sealed class Database : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public SqliteHistoryTransferStore Store => new(this, [new ArkNovaTransferPersistence()]);
        public BoardgameTrackerDbContext CreateDbContext() => new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().UseSqlite(_connection).Options);
        public static async Task<Database> CreateAsync()
        {
            Database database = new();
            await database._connection.OpenAsync(Token);
            await using BoardgameTrackerDbContext context = database.CreateDbContext();
            await context.Database.EnsureCreatedAsync(Token);
            context.Games.Add(new("ark-nova"));
            foreach (int number in Enumerable.Range(1, 14))
                context.ArkNovaZooPlans.Add(new(number));
            await context.SaveChangesAsync(Token);
            return database;
        }
        public async Task<Guid> SeedAsync(string suffix = "")
        {
            await using BoardgameTrackerDbContext db = CreateDbContext();
            Player first = new("Anna" + suffix);
            Player second = new("Guest" + suffix, true);
            second.SetArchived(true);
            Group group = new("Group" + suffix, false, [first.Id, second.Id]);
            Guid game = await db.Games.Select(game => game.Id).SingleAsync(Token);
            DateTimeOffset start = DateTimeOffset.UtcNow.AddHours(-2);
            GameSession gameSession = new(game, group.Id, start, [first.Id, second.Id]);
            gameSession.BeginScoring(start.AddHours(1));
            foreach (GameSessionPlayer player in gameSession.Players)
                player.SetPlacement(1);
            gameSession.Complete(start.AddHours(1));
            db.AddRange(first, second, group, gameSession);
            int index = 0;
            foreach (GameSessionPlayer player in gameSession.Players)
            {
                int option = index++ * 2 + 1;
                BoardgameTracker.Core.Games.ArkNova.GameSessions.ArkNovaGameSessionPlayer detail = new(gameSession.Id, player.PlayerId,
                    index == 1 ? PlayerColor.Blue : PlayerColor.Red, option, option + 1, option);
                detail.SetScoreValues(70, 20, null);
                db.ArkNovaGameSessionPlayers.Add(detail);
            }
            await db.SaveChangesAsync(Token);
            return gameSession.Id;
        }
        public async Task<int> CountAsync()
        {
            await using BoardgameTrackerDbContext db = CreateDbContext();
            return await db.GameSessions.CountAsync(Token);
        }
        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}

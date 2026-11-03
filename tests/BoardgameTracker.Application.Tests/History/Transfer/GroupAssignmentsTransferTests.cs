using System.Text.Json;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.History.Transfer;

public sealed class GroupAssignmentsTransferTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static JsonElement Empty => JsonSerializer.SerializeToElement(new { });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LegacyV1_ImportsRegularGroupAndExportsFalse(bool legacyTemporary)
    {
        await using Database database = await Database.CreateAsync();
        BgtFile file = CreateFile(legacyTemporary);
        using MemoryStream stream = new(JsonSerializer.SerializeToUtf8Bytes(file, BgtFormat.JsonOptions));
        HistoryTransferUseCase useCase = new(database.Store, database.Format);
        ImportPlan plan = await useCase.AnalyzeAsync(stream, Token);
        await useCase.PreviewAsync(plan, Token);
        await useCase.CommitAsync(plan, Token);

        BgtFile exported = await database.Store.ExportAsync(file.GameSessions.Select(session => session.Id).ToArray(), "test", Token);
        Assert.Equal(1, exported.FormatVersion);
        Assert.False(Assert.Single(exported.Groups).IsTemporary);
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Null(verify.Model.FindEntityType(typeof(Group))!.FindProperty("IsTemporary"));
        // Query the real schema too: no legacy status column survives in SQLite.
        await using var command = verify.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Groups') WHERE name = 'IsTemporary'";
        Assert.Equal(0L, Convert.ToInt64(await command.ExecuteScalarAsync(Token)));
        Assert.False((await verify.Groups.SingleAsync(Token)).IsArchived);
    }

    [Fact]
    public async Task NewGroup_MultipleGamesAndRepeatedSessionsCreateOneAssignmentPerPair()
    {
        await using Database database = await Database.CreateAsync();
        BgtFile file = CreateFile();
        ImportPlan plan = ImportPlanner.Analyze(file, await database.Store.ReadSnapshotAsync(Token));
        await database.Store.CommitAsync(plan, Token);
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Guid groupId = file.Groups.Single().Id;
        Guid[] assignedGames = await verify.GroupGames.Where(assignment => assignment.GroupId == groupId)
            .Select(assignment => assignment.GameId).ToArrayAsync(Token);
        Assert.Equal(2, assignedGames.Length);
        Assert.True(assignedGames.ToHashSet().SetEquals(await verify.Games.Select(game => game.Id).ToArrayAsync(Token)));
        Assert.Equal(3, await verify.GameSessions.CountAsync(Token));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task ExistingGroup_KeepsAssignmentsOrderAndArchiveState(bool matchById, bool hasAssignment)
    {
        await using Database database = await Database.CreateAsync();
        BgtFile file = CreateFile();
        Guid localGroupId;
        Guid firstGame;
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            foreach (BgtPlayer player in file.Players)
                seed.Players.Add(new(player.Name, player.IsGuest, player.Id, player.CreatedAt));
            Group group = new(file.Groups[0].Name, file.Players.Reverse().Select(player => player.Id).ToArray(),
                matchById ? file.Groups[0].Id : Guid.NewGuid());
            group.SetArchived(true);
            localGroupId = group.Id;
            seed.Groups.Add(group);
            firstGame = await seed.Games.Where(game => game.Key == "test-first").Select(game => game.Id).SingleAsync(Token);
            if (hasAssignment)
                seed.GroupGames.Add(new(localGroupId, firstGame));
            await seed.SaveChangesAsync(Token);
        }
        ImportPlan plan = ImportPlanner.Analyze(file, await database.Store.ReadSnapshotAsync(Token));
        Assert.False(Assert.Single(plan.Groups).IsNew);
        await database.Store.CommitAsync(plan, Token);
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Guid[] assignedGames = await verify.GroupGames.Select(assignment => assignment.GameId).ToArrayAsync(Token);
        Assert.Equal(hasAssignment ? new[] { firstGame } : Array.Empty<Guid>(), assignedGames);
        Group saved = await verify.Groups.Include(group => group.Players).SingleAsync(Token);
        Assert.Equal(localGroupId, saved.Id);
        Assert.True(saved.IsArchived);
        Assert.Equal(file.Groups[0].Name, saved.Name);
        Assert.Equal(file.Players.Reverse().Select(player => player.Id), saved.Players.OrderBy(player => player.DefaultOrder).Select(player => player.PlayerId));
        Assert.Equal(3, await verify.GameSessions.CountAsync(Token));
    }

    [Fact]
    public async Task KnownSession_DoesNotAssignItsGameToNewGroupAndRepeatedImportRestoresNothing()
    {
        await using Database database = await Database.CreateAsync();
        BgtFile file = CreateFile();
        // A known ID is skipped regardless of its content. Its foreign dependencies
        // must not influence assignments of a newly imported group.
        BgtGameSession known = file.GameSessions[2];
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            Player player = new("Known player");
            Group group = new("Known group", [player.Id]);
            Guid gameId = await seed.Games.Where(game => game.Key == "test-second").Select(game => game.Id).SingleAsync(Token);
            GameSession session = new(gameId, group.Id, known.StartedAt, [player.Id], known.Id);
            session.BeginScoring(known.EndedAt);
            session.Complete(known.ModifiedAt);
            seed.AddRange(player, group, session);
            await seed.SaveChangesAsync(Token);
        }
        ImportPlan plan = ImportPlanner.Analyze(file, await database.Store.ReadSnapshotAsync(Token));
        Assert.True(Assert.Single(plan.Groups).IsNew);
        Assert.Equal(1, plan.Skipped);
        await database.Store.CommitAsync(plan, Token);
        await using (BoardgameTrackerDbContext verify = database.CreateDbContext())
        {
            GroupGame assignment = await verify.GroupGames.SingleAsync(Token);
            Assert.Equal(file.Groups[0].Id, assignment.GroupId);
            Assert.Equal("test-first", (await verify.Games.SingleAsync(game => game.Id == assignment.GameId, Token)).Key);
            await verify.GroupGames.ExecuteDeleteAsync(Token);
        }
        ImportPlan repeated = ImportPlanner.Analyze(file, await database.Store.ReadSnapshotAsync(Token));
        Assert.Empty(repeated.GameSessions);
        await database.Store.CommitAsync(repeated, Token);
        await using BoardgameTrackerDbContext final = database.CreateDbContext();
        Assert.Empty(await final.GroupGames.ToArrayAsync(Token));
        Assert.Equal(3, await final.GameSessions.CountAsync(Token));
    }

    [Fact]
    public async Task FailureAfterAssignmentsWereSaved_RollsBackEntireImport()
    {
        await using Database database = await Database.CreateAsync(failImport: true);
        BgtFile file = CreateFile();
        ImportPlan plan = ImportPlanner.Analyze(file, await database.Store.ReadSnapshotAsync(Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => database.Store.CommitAsync(plan, Token));
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
        Assert.Empty(await verify.Groups.ToArrayAsync(Token));
        Assert.Empty(await verify.Players.ToArrayAsync(Token));
        Assert.Empty(await verify.GameSessions.ToArrayAsync(Token));
        Assert.Empty(await verify.GameSessionPlayers.ToArrayAsync(Token));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("unknown")]
    public async Task LegacyField_RemainsRequiredBooleanAndUnknownFieldsRemainRejected(string invalid)
    {
        await using Database database = await Database.CreateAsync();
        string json = JsonSerializer.Serialize(CreateFile(), BgtFormat.JsonOptions);
        json = invalid switch
        {
            "missing" => json.Replace("\"isTemporary\": false,", ""),
            "null" => json.Replace("\"isTemporary\": false", "\"isTemporary\": null"),
            _ => json.Replace("\"isTemporary\": false", "\"isTemporary\": false, \"unknown\": true")
        };
        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(json));
        await Assert.ThrowsAsync<JsonException>(() => database.Format.ReadAsync(stream, Token));
    }

    private static BgtFile CreateFile(bool legacyTemporary = false)
    {
        BgtPlayer[] players = [new(Guid.NewGuid(), "First", false, false, DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), "Second", false, false, DateTimeOffset.UtcNow)];
        BgtGroup group = new(Guid.NewGuid(), "Imported group", legacyTemporary, false,
            players.Select((player, index) => new BgtGroupPlayer(player.Id, index + 1)).ToArray());
        BgtGame[] games = [new(Guid.NewGuid(), "test-first", 1, Empty), new(Guid.NewGuid(), "test-second", 1, Empty)];
        DateTimeOffset start = DateTimeOffset.UtcNow.AddHours(-2);
        BgtGameSession[] sessions = Enumerable.Range(0, 3).Select(index => new BgtGameSession(Guid.NewGuid(),
            games[index == 2 ? 1 : 0].Id, group.Id, start, start.AddHours(1), start.AddHours(1),
            players.Select((player, order) => new BgtGameSessionPlayer(player.Id, order + 1, 1)).ToArray(), Empty)).ToArray();
        return new(1, "test", DateTimeOffset.UtcNow, games, players, [group], sessions);
    }

    private sealed class Database(bool failImport) : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        private TestGameModule[] Modules => [new("test-first", failImport), new("test-second", failImport)];
        public HistoryTransferPersistence Store => new(this, Modules);
        public BgtFormat Format => new(Modules);
        public BoardgameTrackerDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().UseTestSqlite(_connection).Options);
        public static async Task<Database> CreateAsync(bool failImport = false)
        {
            Database database = new(failImport);
            await database._connection.OpenAsync(Token);
            await using BoardgameTrackerDbContext db = database.CreateDbContext();
            await db.Database.EnsureCreatedAsync(Token);
            db.Games.AddRange(new Game("test-first"), new Game("test-second"));
            await db.SaveChangesAsync(Token);
            return database;
        }
        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }

    // Two minimal modules exercise general transfer behavior with real relational storage.
    private sealed class TestGameModule(string gameKey, bool failImport) : IGameTransferPersistence, IGameTransferSchema
    {
        public string GameKey => gameKey;
        public int SchemaVersion => 1;
        public void Validate(BgtGame game, IReadOnlyList<BgtGameSession> gameSessions) { }
        public Task<JsonElement> ReadSnapshotAsync(BoardgameTrackerDbContext db, CancellationToken cancellationToken) => Task.FromResult(Empty);
        public Task<JsonElement> ExportReferencesAsync(BoardgameTrackerDbContext db, Guid[] gameSessionIds, CancellationToken cancellationToken) => Task.FromResult(Empty);
        public Task<IReadOnlyDictionary<Guid, JsonElement>> ExportGameSessionsAsync(BoardgameTrackerDbContext db, Guid[] gameSessionIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, JsonElement>>(gameSessionIds.ToDictionary(id => id, _ => Empty));
        public Task<(int Existing, int New)> PreviewAsync(BoardgameTrackerDbContext db, BgtGame game, IReadOnlyList<BgtGameSession> gameSessions, CancellationToken cancellationToken) => Task.FromResult((0, 0));
        public async Task ImportAsync(BoardgameTrackerDbContext db, BgtGame game, IReadOnlyList<BgtGameSession> gameSessions,
            IReadOnlyDictionary<Guid, Guid> players, CancellationToken cancellationToken)
        {
            if (failImport)
            {
                await db.SaveChangesAsync(cancellationToken);
                Assert.NotEmpty(await db.GroupGames.AsNoTracking().ToArrayAsync(cancellationToken));
                throw new InvalidOperationException("Simulated module failure after relational writes.");
            }
        }
    }
}

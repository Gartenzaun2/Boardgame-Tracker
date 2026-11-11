using BoardgameTracker.App.Persistence.SQLite;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Application.History;
using BoardgameTracker.App.Features.Players.EditPlayer;
using BoardgameTracker.App.Features.Players.ListPlayers;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.Players;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.GameSessions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Players;

public sealed class PlayerManagementTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false, false, 2)]
    [InlineData(true, false, 3)]
    [InlineData(false, true, 3)]
    [InlineData(true, true, 5)]
    public async Task List_FiltersGuestsAndArchivedIndependently(bool guests, bool archived, int expectedCount)
    {
        await using Database database = await Database.CreateAsync();
        IReadOnlyList<PlayerListItem> rows = await database.Query.ListPlayersAsync(guests, archived, Token);
        Assert.Equal(expectedCount, rows.Count);
        Assert.Contains(rows, player => player.PlayerId == database.Players[0].Id);
        Assert.All(rows, player => Assert.True((guests || !player.IsGuest) && (archived || !player.IsArchived)));
        Assert.Equal(guests && archived, rows.Any(player => player.PlayerId == database.Players[4].Id));
    }

    [Theory]
    [InlineData(GameSessionStatus.Running, true)]
    [InlineData(GameSessionStatus.Scoring, true)]
    [InlineData(GameSessionStatus.Completed, false)]
    public async Task List_UsesLatestStartAcrossAllSessionStatesAndExposesOpenStatus(GameSessionStatus latestStatus, bool isOpen)
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;
        await using Database database = await Database.CreateAsync(latestStatus);
        DateTimeOffset after = DateTimeOffset.UtcNow;
        IReadOnlyList<PlayerListItem> rows = await database.Query.ListPlayersAsync(cancellationToken: Token);
        PlayerListItem player = rows.Single(row => row.PlayerId == database.Players[0].Id);
        Assert.Equal(database.Players[0].CreatedAt, player.CreatedAt);
        Assert.InRange(player.CreatedAt, before, after);
        Assert.Equal(database.Latest.Id, player.LastGameSessionId);
        Assert.Equal(database.Latest.StartedAt, player.LastPlayedAt);
        Assert.Equal("test-game", player.LastGameKey);
        Assert.Equal(latestStatus, player.LastGameSessionStatus);
        Assert.Equal(isOpen, player.LastGameSessionStatus is GameSessionStatus.Running or GameSessionStatus.Scoring);
        PlayerListItem unused = rows.Single(row => row.PlayerId == database.Players[1].Id);
        Assert.Null(unused.LastGameSessionId);
        Assert.Null(unused.LastGameKey);
        Assert.Null(unused.LastPlayedAt);
        Assert.Null(unused.LastGameSessionStatus);
        Assert.Null(typeof(PlayerListViewModel).GetMethod("OpenLastGameSessionAsync"));
    }

    [Fact]
    public async Task Edit_TrimsNameArchivesAndReactivatesWithoutDeletingRelationships()
    {
        await using Database database = await Database.CreateAsync();
        Guid id = database.Players[0].Id;
        DateTimeOffset createdAt = database.Players[0].CreatedAt;
        await database.Edit.UpdatePlayerAsync(id, "  New name  ", true, Token);
        Player player = (await database.Edit.GetAsync(id, Token))!;
        Assert.Equal("New name", player.Name);
        Assert.True(player.IsArchived);
        Assert.False(player.IsGuest);
        Assert.Equal(createdAt, player.CreatedAt);
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        Assert.True(await db.GroupPlayers.AnyAsync(member => member.PlayerId == id, Token));
        Assert.Equal(3, await db.GameSessionPlayers.CountAsync(member => member.PlayerId == id, Token));
        var history = await new CompletedGameSessionsQuery(database).ListAsync(cancellationToken: Token);
        Assert.Equal("New name", Assert.Single(history).Players.Single(member => member.PlayerId == id).Name);
        await database.Edit.UpdatePlayerAsync(id, player.Name, false, Token);
        Assert.False((await database.Edit.GetAsync(id, Token))!.IsArchived);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Edit_RejectsBlankNameThroughDomainValidation(string name)
    {
        await using Database database = await Database.CreateAsync();
        Guid id = database.Players[0].Id;
        await Assert.ThrowsAsync<ArgumentException>(() => database.Edit.UpdatePlayerAsync(id, name, true, Token));
        Player player = (await database.Edit.GetAsync(id, Token))!;
        Assert.Equal("Anna", player.Name);
        Assert.False(player.IsArchived);
    }

    [Fact]
    public async Task Editor_ChangesRemainDraftUntilSave()
    {
        await using Database database = await Database.CreateAsync();
        Guid id = database.Players[0].Id;
        EditPlayerViewModel editor = new(database.Edit, new(database), NullLogger<EditPlayerViewModel>.Instance);
        await editor.LoadAsync(id, Token);
        editor.Name = "Changed";
        editor.IsArchived = true;
        Assert.False((await database.Edit.GetAsync(id, Token))!.IsArchived);
        Assert.Equal("Anna", (await database.Edit.GetAsync(id, Token))!.Name);
        Assert.True(await editor.SaveAsync(Token));
        Assert.True((await database.Edit.GetAsync(id, Token))!.IsArchived);
        Assert.Equal("Changed", (await database.Edit.GetAsync(id, Token))!.Name);
    }

    [Fact]
    public async Task Management_FiltersPlayersAndSwitchesSegmentsWithoutOpeningSessions()
    {
        await using Database database = await Database.CreateAsync();
        HistoryHandler matching = new("test-game");
        HistoryHandler other = new("other-game");
        PlayerListViewModel viewModel = new(database.Query, new(database), NullLogger<PlayerListViewModel>.Instance);
        await viewModel.LoadAsync(Token);
        Assert.False(viewModel.IncludeGuests);
        Assert.False(viewModel.IncludeArchived);
        Assert.Empty(matching.Opened);
        Assert.Empty(matching.Opened);
        viewModel.SelectGroups();
        Assert.True(viewModel.ShowGroups);
        viewModel.SelectPlayers();
        Assert.True(viewModel.ShowPlayers);
        Assert.Empty(other.Opened);
        viewModel.IncludeGuests = true;
        viewModel.IncludeArchived = true;
        await viewModel.LoadAsync(Token);
        Assert.Equal(5, viewModel.Players.Count);
    }

    [Fact]
    public void GeneralPlayerFeature_HasNoGameSpecificReferences()
    {
        System.Reflection.Assembly assembly = typeof(PlayerManagementTests).Assembly;
        string[] resources = assembly.GetManifestResourceNames().Where(name => name.StartsWith("PlayerFeature.", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(resources);
        foreach (string resource in resources)
        {
            using Stream stream = assembly.GetManifestResourceStream(resource)!;
            using StreamReader reader = new(stream);
            Assert.DoesNotContain("ArkNova", reader.ReadToEnd(), StringComparison.Ordinal);
        }
    }

    private sealed class HistoryHandler(string key) : IGameSessionNavigationHandler
    {
        public string GameKey => key;
        public bool CanHandle(string gameKey) => gameKey == key;
        public string DisplayName => key;
        public string BadgeText => DisplayName;
        public Task OpenGroupAsync(string gameKey, Guid groupId, Guid? openGameSessionId, GameSessionStatus? openGameSessionStatus) => throw new NotSupportedException();
        public List<Guid> Opened { get; } = [];
        public Task OpenGameSessionAsync(Guid gameSessionId)
        {
            Opened.Add(gameSessionId);
            return Task.CompletedTask;
        }
    }

    private sealed class Database : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public Player[] Players { get; } = [new("Anna"), new("No games"), new("Guest", true), new("Archived"), new("Archived guest", true)];
        public GameSession Latest { get; private set; } = null!;
        public ListPlayersQuery Query => new(this);
        public EditPlayerUseCase Edit => new(this);
        public BoardgameTrackerDbContext CreateDbContext() => new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().UseTestSqlite(_connection).Options);

        public static async Task<Database> CreateAsync(GameSessionStatus latestStatus = GameSessionStatus.Scoring)
        {
            Database database = new();
            await database._connection.OpenAsync(Token);
            await using BoardgameTrackerDbContext db = database.CreateDbContext();
            await db.Database.EnsureCreatedAsync(Token);
            database.Players[3].SetArchived(true);
            database.Players[4].SetArchived(true);
            Game game = new("test-game", "test-game", "test-game", 1, 8);
            Group group = new("Group", database.Players.Select(player => player.Id).ToArray());
            db.AddRange(database.Players);
            db.AddRange(game, group);
            foreach (GameSessionStatus status in new[] { GameSessionStatus.Completed, GameSessionStatus.Running, GameSessionStatus.Scoring })
            {
                int day = status == latestStatus ? 4 : status == GameSessionStatus.Completed ? 1 : status == GameSessionStatus.Running ? 2 : 3;
                DateTimeOffset start = new(2026, 10, day, 12, 0, 0, TimeSpan.Zero);
                GameSession session = new(game.Id, group.Id, start, [database.Players[0].Id, database.Players[3].Id]);
                if (status != GameSessionStatus.Running)
                    session.BeginScoring(start.AddHours(1));
                if (status == GameSessionStatus.Completed)
                {
                    foreach (GameSessionPlayer player in session.Players)
                        player.SetPlacement(1);
                    session.Complete(start.AddHours(2));
                }
                db.Add(session);
                if (status == latestStatus)
                    database.Latest = session;
            }
            await db.SaveChangesAsync(Token);
            return database;
        }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}

using System.Data.Common;
using System.Text.Json;
using BoardgameTracker.App.Persistence.SQLite;
using BoardgameTracker.Application.GameSessions;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.History.Transfer;
using BoardgameTracker.Application.Games.ArkNova.Persistence.Seeding;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.GameSessions;

public sealed class ArkNovaPersistenceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HistoricalCorrection_UpdatesTimestampAndExportWithoutChangingSetup()
    {
        await using Fixture f = await Fixture.CreateAsync(completed: true);
        ArkNovaHistoryCorrectionUseCase correction = f.Correction(f);
        var draft = await correction.LoadCorrectionDraftAsync(f.Session.Id, Token);
        draft.SetScore(f.A.Id, 91, 7, 8);
        DateTimeOffset before = DateTimeOffset.UtcNow;
        await correction.SaveCorrectionAsync(draft, f.Placements, Token);
        DateTimeOffset after = DateTimeOffset.UtcNow;
        await using BoardgameTrackerDbContext db = f.CreateDbContext();
        GameSession saved = await db.GameSessions.Include(s => s.Players).SingleAsync(Token);
        Assert.InRange(saved.ModifiedAt, before, after);
        Assert.True(saved.ModifiedAt > f.Session.ModifiedAt);
        Assert.Equal(GameSessionStatus.Completed, saved.Status);
        Assert.Equal(f.Session.Id, saved.Id);
        Assert.Equal(f.Session.GameId, saved.GameId);
        Assert.Equal(f.Session.GroupId, saved.GroupId);
        Assert.Equal(f.Session.StartedAt, saved.StartedAt);
        Assert.Equal(f.Session.EndedAt, saved.EndedAt);
        Assert.Equal(new int?[] { 1, 2 }, saved.Players.OrderBy(p => p.Order).Select(p => p.Order));
        var players = await db.ArkNovaGameSessionPlayers.OrderBy(p => p.SelectedZooPlanNumber).ToArrayAsync(Token);
        Assert.Equal(new[] { 1, 2 }, players.Select(p => p.SelectedZooPlanNumber));
        Assert.Equal(91, players.Single(p => p.PlayerId == f.A.Id).Attractiveness);
        Assert.Equal(new[] { PlayerColor.Blue, PlayerColor.Red }, await db.GameSessionPlayerColors.OrderBy(c => c.Color).Select(c => c.Color).ToArrayAsync(Token));
        var exporter = new HistoryTransferPersistence(f, [new ArkNovaTransferPersistence()]);
        BgtFile exported = await exporter.ExportAsync([saved.Id], "test", Token);
        Assert.Equal(saved.ModifiedAt, Assert.Single(exported.GameSessions).ModifiedAt);
    }

    [Fact]
    public async Task HistoricalCorrection_LateInsertFailureRollsBackDeletedCardsScoresAndTimestamp()
    {
        await using Fixture f = await Fixture.CreateAsync(completed: true);
        string before = await f.SnapshotAsync();
        await using (BoardgameTrackerDbContext db = f.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectCorrectionCard BEFORE INSERT ON ArkNovaGameSessionSpecialCardAssignments BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        CommandProbe commands = new();
        ArkNovaHistoryCorrectionUseCase correction = f.Correction(f.Factory(commands));
        var draft = await correction.LoadCorrectionDraftAsync(f.Session.Id, Token);
        draft.SetScore(f.A.Id, 91, 7, 8);
        draft.SetCard(f.B.Id, ArkNovaSpecialCardDefaults.SunBear, true);
        await Assert.ThrowsAsync<DbUpdateException>(() => correction.SaveCorrectionAsync(draft, f.Placements, Token));
        Assert.Equal(1, commands.CompletedAssignmentDeletes);
        Assert.Equal(before, await f.SnapshotAsync());
        Assert.False(draft.Saved);
        await using (BoardgameTrackerDbContext db = f.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectCorrectionCard", Token);
        await correction.SaveCorrectionAsync(draft, f.Placements, Token);
        Assert.True(draft.Saved);
        Assert.NotEqual(before, await f.SnapshotAsync());
    }

    [Theory]
    [InlineData("score")]
    [InlineData("photo")]
    [InlineData("discard")]
    public async Task ScoringWrite_AndIndependentCompletionCannotChangeCompletedHistory(string action)
    {
        await using Fixture f = await Fixture.CreateAsync();
        SaveProbe gate = new() { BlockFirstSave = true };
        Task pending = WriteAsync(f, f.Scoring(f.Factory(gate)), action);
        await gate.FirstSaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        string? completed = null;
        bool blocked;
        try
        {
            blocked = await IsBlockedAsync(() => f.Scoring(f).CompleteAsync(f.Session.Id, f.Placements, DateTimeOffset.UtcNow, Token));
            if (!blocked) completed = await f.SnapshotAsync();
        }
        finally { gate.ReleaseFirstSave.TrySetResult(); }
        Exception? error = await Record.ExceptionAsync(() => pending);
        if (!blocked)
        {
            Assert.Equal(completed, await f.SnapshotAsync());
            Assert.IsType<InvalidOperationException>(error);
        }
        else
        {
            Assert.Null(error);
            if (action == "discard")
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => f.Scoring(f).CompleteAsync(f.Session.Id, f.Placements, DateTimeOffset.UtcNow, Token));
                await using BoardgameTrackerDbContext db = f.CreateDbContext();
                Assert.Empty(await db.GameSessions.ToArrayAsync(Token));
            }
            else
            {
                await f.Scoring(f).CompleteAsync(f.Session.Id, f.Placements, DateTimeOffset.UtcNow, Token);
                await using BoardgameTrackerDbContext db = f.CreateDbContext();
                Assert.Equal(GameSessionStatus.Completed, (await db.GameSessions.SingleAsync(Token)).Status);
                Assert.Equal(91, (await db.ArkNovaGameSessionPlayers.SingleAsync(p => p.PlayerId == f.A.Id, Token)).Attractiveness);
            }
        }
    }

    [Fact]
    public async Task Completion_CannotPersistPlacementsBasedOnScoresChangedByAnotherOperation()
    {
        await using Fixture f = await Fixture.CreateAsync();
        SaveProbe gate = new() { BlockFirstSave = true };
        Task pending = f.Scoring(f.Factory(gate)).CompleteAsync(f.Session.Id, f.Placements, DateTimeOffset.UtcNow, Token);
        await gate.FirstSaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        bool blocked;
        try
        {
            blocked = await IsBlockedAsync(() => f.Scoring(f).SavePlayerScoreAsync(f.Session.Id, f.B.Id, 100, 7, 6, Token));
        }
        finally { gate.ReleaseFirstSave.TrySetResult(); }
        await pending;
        if (blocked)
            await Assert.ThrowsAsync<InvalidOperationException>(() => f.Scoring(f).SavePlayerScoreAsync(f.Session.Id, f.B.Id, 100, 7, 6, Token));
        await using BoardgameTrackerDbContext db = f.CreateDbContext();
        Assert.Equal(GameSessionStatus.Completed, (await db.GameSessions.SingleAsync(Token)).Status);
        Assert.Equal(70, (await db.ArkNovaGameSessionPlayers.SingleAsync(p => p.PlayerId == f.B.Id, Token)).Attractiveness);
        Assert.Equal(2, (await db.GameSessionPlayers.SingleAsync(p => p.PlayerId == f.B.Id, Token)).Placement);
    }

    [Fact]
    public async Task Completion_RevalidatesAnEarlierUiPlanAgainstCurrentScores()
    {
        await using Fixture f = await Fixture.CreateAsync();
        ArkNovaScoringUseCase scoring = f.Scoring(f);
        var plan = await scoring.PrepareCompletionAsync(f.Session.Id, Token);
        await scoring.SavePlayerScoreAsync(f.Session.Id, f.B.Id, 100, 7, 6, Token);
        string before = await f.SnapshotAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => scoring.CompleteAsync(f.Session.Id, plan.DefaultPlacements, DateTimeOffset.UtcNow, Token));
        Assert.Equal(before, await f.SnapshotAsync());
        var current = await scoring.PrepareCompletionAsync(f.Session.Id, Token);
        await scoring.CompleteAsync(f.Session.Id, current.DefaultPlacements, DateTimeOffset.UtcNow, Token);
        await using BoardgameTrackerDbContext db = f.CreateDbContext();
        Assert.Equal(1, (await db.GameSessionPlayers.SingleAsync(p => p.PlayerId == f.B.Id, Token)).Placement);
    }

    [Theory]
    [InlineData("discard")]
    [InlineData("begin-scoring")]
    public async Task RunningWrite_AndIndependentCompletionCannotDeleteOrReopenHistory(string action)
    {
        await using Fixture f = await Fixture.CreateAsync(running: true);
        SaveProbe gate = new() { BlockFirstSave = true };
        RunningGameSessionUseCase running = new(f.Factory(gate));
        Task pending = action == "discard" ? running.DiscardAsync(f.Session.Id, Token)
            : running.BeginScoringAsync(f.Session.Id, f.Session.StartedAt.AddHours(1), Token);
        await gate.FirstSaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        bool blocked;
        string? completed = null;
        try
        {
            blocked = await IsBlockedAsync(async () =>
            {
                await new RunningGameSessionUseCase(f).BeginScoringAsync(f.Session.Id, f.Session.StartedAt.AddHours(1), Token);
                await f.Scoring(f).CompleteAsync(f.Session.Id, f.Placements, DateTimeOffset.UtcNow, Token);
            });
            if (!blocked) completed = await f.SnapshotAsync();
        }
        finally { gate.ReleaseFirstSave.TrySetResult(); }
        Exception? error = await Record.ExceptionAsync(() => pending);
        if (!blocked)
        {
            Assert.Equal(completed, await f.SnapshotAsync());
            Assert.IsType<InvalidOperationException>(error);
        }
        else
        {
            Assert.Null(error);
            await using BoardgameTrackerDbContext db = f.CreateDbContext();
            if (action == "discard") Assert.Empty(await db.GameSessions.ToArrayAsync(Token));
            else
            {
                await f.Scoring(f).CompleteAsync(f.Session.Id, f.Placements, DateTimeOffset.UtcNow, Token);
                Assert.Equal(GameSessionStatus.Completed, (await db.GameSessions.SingleAsync(Token)).Status);
            }
        }
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("remove")]
    public async Task CardWrite_DoesNotApplyAnEarlierStatusCheckAfterIndependentCompletion(string action)
    {
        await using Fixture f = await Fixture.CreateAsync();
        string? completed = null;
        ContextFactory factory = f.Factory();
        factory.BeforeCreateAsync = async count =>
        {
            if (count != 2) return;
            await f.Scoring(f).CompleteAsync(f.Session.Id, f.Placements, DateTimeOffset.UtcNow, Token);
            completed = await f.SnapshotAsync();
        };
        ArkNovaScoringUseCase scoring = f.Scoring(factory);
        Exception? error = await Record.ExceptionAsync(() => action == "assign"
            ? scoring.AssignSpecialCardAsync(f.Session.Id, f.B.Id, ArkNovaSpecialCardDefaults.SunBear, Token)
            : scoring.RemoveSpecialCardAsync(f.Session.Id, f.A.Id, ArkNovaSpecialCardDefaults.AfricanElephant, Token));
        factory.BeforeCreateAsync = null;
        if (completed is not null)
        {
            Assert.Equal(completed, await f.SnapshotAsync());
            Assert.IsType<InvalidOperationException>(error);
        }
        else
        {
            Assert.Null(error);
            await f.Scoring(f).CompleteAsync(f.Session.Id, f.Placements, DateTimeOffset.UtcNow, Token);
            await Assert.ThrowsAsync<InvalidOperationException>(() => scoring.AssignSpecialCardAsync(f.Session.Id, f.B.Id, ArkNovaSpecialCardDefaults.SunBear, Token));
        }
    }

    [Theory]
    [InlineData("score", 2)]
    [InlineData("photo", 2)]
    [InlineData("discard", 1)]
    public async Task StatusOnlyWrite_UsesTargetedReadsAndPreservesResult(string action, int expectedReads)
    {
        await using Fixture f = await Fixture.CreateAsync();
        CommandProbe commands = new();
        await WriteAsync(f, f.Scoring(f.Factory(commands)), action);
        Assert.Equal(expectedReads, commands.Selects.Count);
        Assert.DoesNotContain(commands.Selects, sql => sql.Contains("ArkNovaUserContent", StringComparison.Ordinal)
            || sql.Contains("ArkNovaSpecialCards", StringComparison.Ordinal));
        await using BoardgameTrackerDbContext db = f.CreateDbContext();
        if (action == "discard") Assert.Empty(await db.GameSessions.ToArrayAsync(Token));
        else Assert.Equal(91, (await db.ArkNovaGameSessionPlayers.SingleAsync(p => p.PlayerId == f.A.Id, Token)).Attractiveness);
    }

    [Fact]
    public async Task HistoricalCardValidation_ReadCountDoesNotGrowWithAssignments()
    {
        int one = await CorrectionReadsAsync(false);
        int four = await CorrectionReadsAsync(true);
        Assert.Equal(one, four);
    }

    private static async Task<int> CorrectionReadsAsync(bool multiple)
    {
        await using Fixture f = await Fixture.CreateAsync(completed: true);
        CommandProbe commands = new();
        ArkNovaHistoryCorrectionUseCase correction = f.Correction(f.Factory(commands));
        var draft = await correction.LoadCorrectionDraftAsync(f.Session.Id, Token);
        if (multiple)
        {
            draft.SetCard(f.B.Id, ArkNovaSpecialCardDefaults.SunBear, true);
            draft.SetCard(f.A.Id, ArkNovaSpecialCardDefaults.BaldEagle, true);
            draft.SetCard(f.B.Id, ArkNovaSpecialCardDefaults.GoldenEagle, true);
        }
        commands.Selects.Clear();
        await correction.SaveCorrectionAsync(draft, f.Placements, Token);
        await using BoardgameTrackerDbContext db = f.CreateDbContext();
        Assert.Equal(multiple ? 4 : 1, await db.ArkNovaGameSessionSpecialCardAssignments.CountAsync(Token));
        return commands.Selects.Count;
    }

    private static Task WriteAsync(Fixture f, ArkNovaScoringUseCase scoring, string action) => action switch
    {
        "score" => scoring.SavePlayerScoreAsync(f.Session.Id, f.A.Id, 91, 7, 8, Token),
        "photo" => scoring.SaveConfirmedPhotoScoresAsync(f.Session.Id, [new(f.A.Id, 91, 7, 8)], Token),
        "discard" => scoring.DiscardAsync(f.Session.Id, Token),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    private static async Task<bool> IsBlockedAsync(Func<Task> operation)
    {
        try { await operation(); return false; }
        catch (SqliteException error) when (error.SqliteErrorCode is 5 or 6) { return true; }
    }

    private sealed class ContextFactory(string connectionString, params IInterceptor[] interceptors) : IDbContextFactory<BoardgameTrackerDbContext>
    {
        private int _created;
        public Func<int, Task>? BeforeCreateAsync { get; set; }
        public BoardgameTrackerDbContext CreateDbContext()
        {
            DbContextOptionsBuilder<BoardgameTrackerDbContext> options = new();
            SqliteServiceCollectionExtensions.Configure(options, connectionString);
            options.AddInterceptors(interceptors);
            return new(options.Options);
        }
        public async Task<BoardgameTrackerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            if (BeforeCreateAsync is not null) await BeforeCreateAsync(++_created);
            return CreateDbContext();
        }
    }

    private sealed class Fixture(string connectionString) : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        private readonly SqliteConnection _anchor = new(connectionString);
        public Player A { get; } = new("A");
        public Player B { get; } = new("B");
        public GameSession Session { get; private set; } = null!;
        public Dictionary<Guid, int> Placements => new() { [A.Id] = 1, [B.Id] = 2 };
        public ContextFactory Factory(params IInterceptor[] interceptors) => new(connectionString, interceptors);
        public BoardgameTrackerDbContext CreateDbContext() => Factory().CreateDbContext();
        public ArkNovaScoringUseCase Scoring(IDbContextFactory<BoardgameTrackerDbContext> factory) => new(factory, new(factory, TestGameContent.Create()));
        public ArkNovaHistoryCorrectionUseCase Correction(IDbContextFactory<BoardgameTrackerDbContext> factory) => new(factory, new(factory, TestGameContent.Create()));
        public static async Task<Fixture> CreateAsync(bool running = false, bool completed = false)
        {
            Fixture f = new(new SqliteConnectionStringBuilder { DataSource = Guid.NewGuid().ToString(), Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared, DefaultTimeout = 1 }.ConnectionString);
            await f._anchor.OpenAsync(Token);
            await new DatabaseInitializer(f, [new ArkNovaReferenceDataSeeder()]).InitializeAsync(Token);
            await using BoardgameTrackerDbContext db = f.CreateDbContext();
            Guid gameId = await db.Games.Select(g => g.Id).SingleAsync(Token);
            Group group = new("Group", [f.A.Id, f.B.Id]);
            f.Session = new(gameId, group.Id, new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), [f.A.Id, f.B.Id]);
            if (!running) f.Session.BeginScoring(f.Session.StartedAt.AddHours(1));
            if (completed)
            {
                foreach (GameSessionPlayer p in f.Session.Players) p.SetPlacement(f.Placements[p.PlayerId]);
                f.Session.Complete(f.Session.StartedAt.AddHours(2));
            }
            ArkNovaGameSessionPlayer a = new(f.Session.Id, f.A.Id, 1);
            ArkNovaGameSessionPlayer b = new(f.Session.Id, f.B.Id, 2);
            a.SetScoreValues(90, 7, 5);
            b.SetScoreValues(70, 7, 6);
            db.AddRange(f.A, f.B, group, f.Session, a, b);
            db.GameSessionPlayerColors.AddRange(new(f.Session.Id, f.A.Id, PlayerColor.Blue), new(f.Session.Id, f.B.Id, PlayerColor.Red));
            db.ArkNovaGameSessionSpecialCardAssignments.Add(new(f.Session.Id, f.A.Id, ArkNovaSpecialCardDefaults.AfricanElephant));
            await db.SaveChangesAsync(Token);
            return f;
        }
        public async Task<string> SnapshotAsync()
        {
            await using BoardgameTrackerDbContext db = CreateDbContext();
            var sessions = await db.GameSessions.Select(s => new { s.Id, s.GameId, s.GroupId, s.Status, s.StartedAt, s.EndedAt, s.ModifiedAt }).ToArrayAsync(Token);
            var participants = await db.GameSessionPlayers.OrderBy(p => p.PlayerId).Select(p => new { p.PlayerId, p.Order, p.Placement }).ToArrayAsync(Token);
            var scores = await db.ArkNovaGameSessionPlayers.OrderBy(p => p.PlayerId).Select(p => new { p.PlayerId, p.SelectedZooPlanNumber, p.Attractiveness, p.Conservation, p.Reputation }).ToArrayAsync(Token);
            var colors = await db.GameSessionPlayerColors.OrderBy(p => p.PlayerId).Select(p => new { p.PlayerId, p.Color }).ToArrayAsync(Token);
            var cards = await db.ArkNovaGameSessionSpecialCardAssignments.OrderBy(p => p.PlayerId).ThenBy(p => p.SpecialCardId).Select(p => new { p.PlayerId, p.SpecialCardId }).ToArrayAsync(Token);
            return JsonSerializer.Serialize(new { sessions, participants, scores, colors, cards });
        }
        public ValueTask DisposeAsync() => _anchor.DisposeAsync();
    }

    private sealed class CommandProbe : DbCommandInterceptor
    {
        public List<string> Selects { get; } = [];
        public int CompletedAssignmentDeletes { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) Selects.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)
                && command.CommandText.Contains("ArkNovaGameSessionSpecialCardAssignments", StringComparison.Ordinal)) CompletedAssignmentDeletes++;
            return ValueTask.FromResult(result);
        }
    }
}

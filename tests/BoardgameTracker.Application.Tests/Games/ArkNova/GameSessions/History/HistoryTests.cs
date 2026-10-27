using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Games.ArkNova.Players;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite.GameSessions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace BoardgameTracker.Application.Tests.Games.ArkNova.GameSessions.History;

public sealed class HistoryTests
{
    [Fact]
    public async Task History_FiltersCompletedSortsDatesAndPlacementsAndUsesCurrentNames()
    {
        await using var fixture = await Fixture.CreateAsync();
        var newer = await fixture.AddAsync(GameSessionStatus.Completed, 2);
        await fixture.AddAsync(GameSessionStatus.Running, 3);
        await fixture.AddAsync(GameSessionStatus.Scoring, 4);
        var rows = await new SqliteHistoryQuery(fixture).ListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, rows.Count);
        Assert.Equal(newer.Id, rows[0].GameSessionId);
        Assert.Equal(fixture.B.Id, rows[0].Players[0].PlayerId);
        Assert.Empty(await new SqliteHistoryQuery(fixture).ListAsync("other"));
        Assert.Empty(await new SqliteHistoryQuery(fixture).ListAsync(groupId: Guid.NewGuid()));
        Assert.Equal(2, (await new SqliteHistoryQuery(fixture).ListAsync(ArkNovaGame.Key, fixture.Group.Id)).Count);
        await using var db = fixture.CreateDbContext();
        var player = await db.Players.SingleAsync(p => p.Id == fixture.B.Id);
        player.SetName("Renamed");
        var group = await db.Groups.SingleAsync(); group.SetName("Current group");
        await db.SaveChangesAsync();
        rows = await new SqliteHistoryQuery(fixture).ListAsync();
        Assert.Equal("Renamed", rows[0].Players[0].Name);
        Assert.Equal("Current group", rows[0].GroupName);
    }
    [Fact]
    public async Task Correction_IsAtomicPreservesCompletedAndSetupAndRecalculatesTies()
    {
        await using var f = await Fixture.CreateAsync();
        var useCase = new ArkNovaCorrectionUseCase(new SqliteArkNovaScoringDataAccess(f), new SqliteArkNovaCorrectionDataAccess(f));
        var draft = await useCase.BeginAsync(f.Completed.Id);
        Assert.Equal(f.B.Id, draft.Data.Players[0].PlayerId);
        draft.SetScore(f.A.Id, 80, 7, 10);
        draft.SetCard(f.B.Id, ArkNovaSpecialCard.SunBear, true);
        draft.SetCard(f.A.Id, ArkNovaSpecialCard.AfricanElephant, false);
        var before = await useCase.BeginAsync(f.Completed.Id);
        Assert.Equal(70, before.Data.Players.Single(p => p.PlayerId == f.A.Id).Attractiveness);
        Assert.Contains(before.Data.SpecialCards, c => c.SpecialCard == ArkNovaSpecialCard.AfricanElephant);
        var plan = useCase.Prepare(draft);
        Assert.True(plan.HasTies);
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.SaveAsync(draft, new Dictionary<Guid, int> { [f.A.Id] = 4, [f.B.Id] = 1 }));
        await useCase.SaveAsync(draft, new Dictionary<Guid, int> { [f.A.Id] = 1, [f.B.Id] = 2 });
        var saved = await useCase.BeginAsync(f.Completed.Id);
        Assert.Equal(f.A.Id, saved.Data.Players[0].PlayerId);
        Assert.Equal(GameSessionStatus.Completed, saved.Data.Status);
        Assert.Equal(f.Completed.StartedAt, saved.Data.StartedAt);
        Assert.Equal(f.Completed.EndedAt, saved.Data.EndedAt);
        Assert.Contains(saved.Data.SpecialCards, c => c.PlayerId == f.B.Id && c.SpecialCard == ArkNovaSpecialCard.SunBear);
        Assert.DoesNotContain(saved.Data.SpecialCards, c => c.SpecialCard == ArkNovaSpecialCard.AfricanElephant);
        Assert.Equal(ArkNovaPlayerColor.Blue, saved.Data.Players.Single(p => p.PlayerId == f.A.Id).Color);
        Assert.Equal(1, saved.Data.Players.Single(p => p.PlayerId == f.A.Id).ZooPlanNumber);
        Assert.False(useCase.Prepare(saved).HasTies); // Existing resolved tie preserved.
        saved.SetScore(f.A.Id, 90, 7, null);
        var noTie = useCase.Prepare(saved);
        Assert.False(noTie.HasTies);
        Assert.Equal(1, noTie.DefaultPlacements[f.A.Id]);
        await useCase.SaveAsync(saved, noTie.DefaultPlacements);
        var read = new SqliteArkNovaScoringDataAccess(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ArkNovaScoringUseCase(read).SavePlayerScoreAsync(f.Completed.Id, f.A.Id, 1, 1, null));
    }
    [Fact]
    public async Task Delete_RemovesCompletedDependentsButKeepsPlayersAndGroup_RejectsRunning()
    {
        await using var f = await Fixture.CreateAsync();
        var useCase = new ArkNovaCorrectionUseCase(new SqliteArkNovaScoringDataAccess(f), new SqliteArkNovaCorrectionDataAccess(f));
        var running = await f.AddAsync(GameSessionStatus.Running, 3);
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.DeleteAsync(running.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.BeginAsync(running.Id));
        await useCase.DeleteAsync(f.Completed.Id);
        await using var db = f.CreateDbContext();
        Assert.False(await db.GameSessionPlayers.AnyAsync(p => p.GameSessionId == f.Completed.Id));
        Assert.False(await db.ArkNovaGameSessionPlayers.AnyAsync(p => p.GameSessionId == f.Completed.Id));
        Assert.False(await db.ArkNovaGameSessionSpecialCardAssignments.AnyAsync(p => p.GameSessionId == f.Completed.Id));
        Assert.Equal(2, await db.Players.CountAsync()); Assert.Equal(1, await db.Groups.CountAsync());
    }
    private sealed class Fixture : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public Player A { get; } = new("Anna"); public Player B { get; } = new("Ben");
        public Group Group { get; private set; } = null!;
        private Game game = new(ArkNovaGame.Key);
        public GameSession Completed { get; private set; } = null!;
        public BoardgameTrackerDbContext CreateDbContext() => new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().UseSqlite(connection).Options);
        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture(); await f.connection.OpenAsync();
            await using var db = f.CreateDbContext(); await db.Database.EnsureCreatedAsync();
            f.Group = new("Group", false, [f.A.Id, f.B.Id]);
            db.AddRange(f.A, f.B, f.Group, f.game, new ArkNovaZooPlan(1), new ArkNovaZooPlan(2), new ArkNovaZooPlan(3), new ArkNovaZooPlan(4));
            await db.SaveChangesAsync(); f.Completed = await f.AddAsync(GameSessionStatus.Completed, 1); return f;
        }
        public async Task<GameSession> AddAsync(GameSessionStatus status, int day)
        {
            var start = new DateTimeOffset(2026, 10, day, 12, 0, 0, TimeSpan.Zero);
            GameSession session = new(game.Id, Group.Id, start, [A.Id, B.Id]);
            if (status != GameSessionStatus.Running) session.BeginScoring(start.AddHours(1));
            if (status == GameSessionStatus.Completed) { session.Players.Single(p => p.PlayerId == A.Id).SetPlacement(2); session.Players.Single(p => p.PlayerId == B.Id).SetPlacement(1); session.Complete(start.AddHours(2)); }
            await using var db = CreateDbContext(); db.Add(session);
            var a = new ArkNovaGameSessionPlayer(session.Id, A.Id, ArkNovaPlayerColor.Blue, 1, 2, 1); a.SetScoreValues(70, 7, null);
            var b = new ArkNovaGameSessionPlayer(session.Id, B.Id, ArkNovaPlayerColor.Red, 3, 4, 3); b.SetScoreValues(80, 7, null);
            db.AddRange(a, b, new ArkNovaGameSessionSpecialCardAssignment(session.Id, A.Id, ArkNovaSpecialCard.AfricanElephant));
            await db.SaveChangesAsync(); return session;
        }
        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }
}

using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.App.Persistence.SQLite;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Application.History;

using BoardgameTracker.Application.Games.ArkNova.Persistence.Seeding;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.GameSessions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace BoardgameTracker.Application.Tests.Games.ArkNova.GameSessions.History;

public sealed class HistoryTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task History_FiltersCompletedSortsDatesAndPlacementsAndUsesCurrentNames()
    {
        await using var fixture = await Fixture.CreateAsync(Token);
        var newer = await fixture.AddAsync(GameSessionStatus.Completed, 2, Token);
        await fixture.AddAsync(GameSessionStatus.Running, 3, Token);
        await fixture.AddAsync(GameSessionStatus.Scoring, 4, Token);
        var rows = await new CompletedGameSessionsQuery(fixture).ListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, rows.Count);
        Assert.Equal(newer.Id, rows[0].GameSessionId);
        Assert.Equal(fixture.B.Id, rows[0].Players[0].PlayerId);
        Assert.Empty(await new CompletedGameSessionsQuery(fixture).ListAsync("other", cancellationToken: Token));
        Assert.Empty(await new CompletedGameSessionsQuery(fixture).ListAsync(groupId: Guid.NewGuid(), cancellationToken: Token));
        Assert.Equal(2, (await new CompletedGameSessionsQuery(fixture).ListAsync(ArkNovaGame.Key, fixture.Group.Id, Token)).Count);
        await using var db = fixture.CreateDbContext();
        var player = await db.Players.SingleAsync(p => p.Id == fixture.B.Id, Token);
        player.SetName("Renamed");
        var group = await db.Groups.SingleAsync(Token);
        group.SetName("Current group");
        await db.SaveChangesAsync(Token);
        rows = await new CompletedGameSessionsQuery(fixture).ListAsync(cancellationToken: Token);
        Assert.Equal("Renamed", rows[0].Players[0].Name);
        Assert.Equal("Current group", rows[0].GroupName);
    }
    [Fact]
    public async Task Correction_IsAtomicPreservesCompletedAndSetupAndRecalculatesTies()
    {
        await using var f = await Fixture.CreateAsync(Token);
        var useCase = new ArkNovaHistoryCorrectionUseCase(f, new(f));
        var draft = await useCase.LoadCorrectionDraftAsync(f.Completed.Id, Token);
        Assert.Equal(f.B.Id, draft.Data.Players[0].PlayerId);
        draft.SetScore(f.A.Id, 80, 7, 10);
        draft.SetCard(f.B.Id, ArkNovaSpecialCardDefaults.SunBear, true);
        draft.SetCard(f.A.Id, ArkNovaSpecialCardDefaults.AfricanElephant, false);
        var before = await useCase.LoadCorrectionDraftAsync(f.Completed.Id, Token);
        Assert.Equal(70, before.Data.Players.Single(p => p.PlayerId == f.A.Id).Attractiveness);
        Assert.Contains(before.Data.SpecialCards, c => c.SpecialCardId == ArkNovaSpecialCardDefaults.AfricanElephant);
        var plan = useCase.CreateCorrectionCompletionPlan(draft);
        Assert.True(plan.HasTies);
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.SaveCorrectionAsync(draft, new Dictionary<Guid, int> { [f.A.Id] = 4, [f.B.Id] = 1 }, Token));
        await useCase.SaveCorrectionAsync(draft, new Dictionary<Guid, int> { [f.A.Id] = 1, [f.B.Id] = 2 }, Token);
        var saved = await useCase.LoadCorrectionDraftAsync(f.Completed.Id, Token);
        Assert.Equal(f.A.Id, saved.Data.Players[0].PlayerId);
        Assert.Equal(GameSessionStatus.Completed, saved.Data.Status);
        Assert.Equal(f.Completed.StartedAt, saved.Data.StartedAt);
        Assert.Equal(f.Completed.EndedAt, saved.Data.EndedAt);
        Assert.Contains(saved.Data.SpecialCards, c => c.PlayerId == f.B.Id && c.SpecialCardId == ArkNovaSpecialCardDefaults.SunBear);
        Assert.DoesNotContain(saved.Data.SpecialCards, c => c.SpecialCardId == ArkNovaSpecialCardDefaults.AfricanElephant);
        Assert.Equal(PlayerColor.Blue, saved.Data.Players.Single(p => p.PlayerId == f.A.Id).Color);
        Assert.Equal(1, saved.Data.Players.Single(p => p.PlayerId == f.A.Id).ZooPlanNumber);
        Assert.False(useCase.CreateCorrectionCompletionPlan(saved).HasTies); // Existing resolved tie preserved.
        saved.SetScore(f.A.Id, 90, 7, null);
        var noTie = useCase.CreateCorrectionCompletionPlan(saved);
        Assert.False(noTie.HasTies);
        Assert.Equal(1, noTie.DefaultPlacements[f.A.Id]);
        await useCase.SaveCorrectionAsync(saved, noTie.DefaultPlacements, Token);
        var read = new ArkNovaGameSessionResultQuery(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ArkNovaScoringUseCase(f, read).SavePlayerScoreAsync(f.Completed.Id, f.A.Id, 1, 1, null, Token));
    }
    [Fact]
    public async Task Delete_RemovesCompletedDependentsButKeepsPlayersAndGroup_RejectsRunning()
    {
        await using var f = await Fixture.CreateAsync(Token);
        var useCase = new ArkNovaHistoryCorrectionUseCase(f, new(f));
        var running = await f.AddAsync(GameSessionStatus.Running, 3, Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.DeleteCompletedGameSessionAsync(running.Id, Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.LoadCorrectionDraftAsync(running.Id, Token));
        await useCase.DeleteCompletedGameSessionAsync(f.Completed.Id, Token);
        await using var db = f.CreateDbContext();
        Assert.False(await db.GameSessionPlayers.AnyAsync(p => p.GameSessionId == f.Completed.Id, Token));
        Assert.False(await db.ArkNovaGameSessionPlayers.AnyAsync(p => p.GameSessionId == f.Completed.Id, Token));
        Assert.False(await db.ArkNovaGameSessionSpecialCardAssignments.AnyAsync(p => p.GameSessionId == f.Completed.Id, Token));
        Assert.Equal(2, await db.Players.CountAsync(Token));
        Assert.Equal(1, await db.Groups.CountAsync(Token));
    }
    private sealed class Fixture : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public Player A { get; } = new("Anna"); public Player B { get; } = new("Ben");
        public Group Group { get; private set; } = null!;
        private Game game = new(ArkNovaGame.Key);
        public GameSession Completed { get; private set; } = null!;
        public BoardgameTrackerDbContext CreateDbContext() => new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().UseTestSqlite(connection).Options);
        public static async Task<Fixture> CreateAsync(CancellationToken cancellationToken)
        {
            Fixture f = new();
            await f.connection.OpenAsync(cancellationToken);
            await using var db = f.CreateDbContext();
            await db.Database.EnsureCreatedAsync(cancellationToken);
            db.AddRange(ArkNovaSpecialCardDefaults.Create());
            f.Group = new("Group", false, [f.A.Id, f.B.Id]);
            db.AddRange(f.A, f.B, f.Group, f.game, new ArkNovaZooPlan(1), new ArkNovaZooPlan(2), new ArkNovaZooPlan(3), new ArkNovaZooPlan(4));
            await db.SaveChangesAsync(cancellationToken);
            f.Completed = await f.AddAsync(GameSessionStatus.Completed, 1, cancellationToken);
            return f;
        }
        public async Task<GameSession> AddAsync(GameSessionStatus status, int day, CancellationToken cancellationToken)
        {
            var start = new DateTimeOffset(2026, 10, day, 12, 0, 0, TimeSpan.Zero);
            GameSession session = new(game.Id, Group.Id, start, [A.Id, B.Id]);
            if (status != GameSessionStatus.Running) session.BeginScoring(start.AddHours(1));
            if (status == GameSessionStatus.Completed) { session.Players.Single(p => p.PlayerId == A.Id).SetPlacement(2); session.Players.Single(p => p.PlayerId == B.Id).SetPlacement(1); session.Complete(start.AddHours(2)); }
            await using var db = CreateDbContext(); db.Add(session);
            var a = new ArkNovaGameSessionPlayer(session.Id, A.Id, PlayerColor.Blue, 1, 2, 1); a.SetScoreValues(70, 7, null);
            var b = new ArkNovaGameSessionPlayer(session.Id, B.Id, PlayerColor.Red, 3, 4, 3); b.SetScoreValues(80, 7, null);
            db.AddRange(a, b, new ArkNovaGameSessionSpecialCardAssignment(session.Id, A.Id, ArkNovaSpecialCardDefaults.AfricanElephant));
            await db.SaveChangesAsync(cancellationToken);
            return session;
        }
        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }
}

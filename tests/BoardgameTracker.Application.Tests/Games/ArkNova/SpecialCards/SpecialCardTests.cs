using BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;
using Microsoft.Extensions.Logging.Abstractions;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.Seeding;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.SpecialCards;

public sealed class SpecialCardTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Editor_AllowsRepeatedSymbolsAndCompactsClearedPositions()
    {
        await using SpecialCardDatabase f = await SpecialCardDatabase.CreateAsync();
        ArkNovaSpecialCardEditorViewModel editor = new(f.Cards, NullLogger<ArkNovaSpecialCardEditorViewModel>.Instance);
        await editor.LoadAsync(null);
        Assert.False(editor.CanSave);
        Assert.False(editor.CanChooseSymbol2);
        editor.NameText = "Dreifach";
        for (int index = 0; index < 3; index++)
        {
            editor.OpenSymbolPicker(index);
            editor.SelectSymbol(ArkNovaCardSymbol.Africa);
        }
        Assert.Equal(new[] { ArkNovaCardSymbol.Africa, ArkNovaCardSymbol.Africa, ArkNovaCardSymbol.Africa }, editor.Preview.Symbols);
        editor.OpenSymbolPicker(0);
        editor.SelectSymbol(null);
        Assert.Equal(2, editor.Preview.Symbols.Count);
        Assert.Equal(ArkNovaCardSymbol.Africa, editor.Preview.Symbol1);
        Assert.Equal(ArkNovaCardSymbol.Africa, editor.Preview.Symbol2);
        Assert.Null(editor.Preview.Symbol3);
        Assert.True(await editor.SaveAsync());
        Assert.Contains(await f.Cards.ListAsync(cancellationToken: Token), c => c.Name == "Dreifach" && c.Symbols.Count == 2);
    }

    [Fact]
    public async Task Editor_EditReusesIdentityAndPreservesArchivedState()
    {
        await using SpecialCardDatabase f = await SpecialCardDatabase.CreateAsync();
        Guid id = ArkNovaSpecialCardDefaults.SunBear;
        await f.Cards.SetArchivedAsync(id, true, Token);
        ArkNovaSpecialCardEditorViewModel editor = new(f.Cards, NullLogger<ArkNovaSpecialCardEditorViewModel>.Instance);
        await editor.LoadAsync(id);
        editor.NameText = "Mein Bär";
        Assert.True(await editor.SaveAsync());
        ArkNovaSpecialCardData edited = (await f.Cards.ListAsync(true, Token)).Single(c => c.Id == id);
        Assert.Equal("Mein Bär", edited.Name);
        Assert.True(edited.IsArchived);
        Assert.Equal(5, (await f.Cards.ListAsync(true, Token)).Count);
    }

    [Fact]
    public async Task Management_CreateEditArchiveUnarchive_UsesCurrentDataAndActiveList()
    {
        await using SpecialCardDatabase f = await SpecialCardDatabase.CreateAsync();
        Assert.Equal(5, (await f.Cards.ListAsync(cancellationToken: Token)).Count);
        Guid id = await f.Cards.CreateAsync("  Nashorn  ", ArkNovaCardSymbol.Africa, ArkNovaCardSymbol.Predator, ArkNovaCardSymbol.Bear, Token);
        Assert.Equal("Nashorn", (await f.Cards.ListAsync(cancellationToken: Token)).Single(c => c.Id == id).Name);
        await f.Cards.UpdateAsync(id, "Ohne Symbole", null, null, null, Token);
        Assert.Empty((await f.Cards.ListAsync(cancellationToken: Token)).Single(c => c.Id == id).Symbols);
        await f.Cards.UpdateAsync(id, "Doppelt", ArkNovaCardSymbol.Africa, ArkNovaCardSymbol.Africa, ArkNovaCardSymbol.Herbivore, Token);
        await f.Cards.SetArchivedAsync(id, true, Token);
        Assert.DoesNotContain(await f.Cards.ListAsync(cancellationToken: Token), c => c.Id == id);
        Assert.True((await f.Cards.ListAsync(true, Token)).Single(c => c.Id == id).IsArchived);
        await f.Cards.SetArchivedAsync(id, false, Token);
        Assert.Equal(new[] { ArkNovaCardSymbol.Africa, ArkNovaCardSymbol.Africa, ArkNovaCardSymbol.Herbivore },
            (await f.Cards.ListAsync(cancellationToken: Token)).Single(c => c.Id == id).Symbols);
        await f.Cards.UpdateAsync(id, "Forschung", ArkNovaCardSymbol.Science, null, null, Token);
        Assert.Equal(ArkNovaCardSymbol.Science, (await f.Cards.ListAsync(cancellationToken: Token)).Single(c => c.Id == id).Symbol1);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Cards.CreateAsync(" ", null, null, null, Token));
        await Assert.ThrowsAsync<ArgumentException>(() => f.Cards.UpdateAsync(id, "Gap", null, ArkNovaCardSymbol.Bird, null, Token));
    }

    [Fact]
    public async Task Scoring_RejectsArchivedAndUnknownCards_AllowsSameCardForMultiplePlayers()
    {
        await using SpecialCardDatabase f = await SpecialCardDatabase.CreateAsync();
        GameSession session = await f.AddSessionAsync(false);
        ArkNovaScoringUseCase scoring = new(f.Scoring);
        Guid card = ArkNovaSpecialCardDefaults.SunBear;
        await scoring.AssignSpecialCardAsync(session.Id, f.A.Id, card, Token);
        await scoring.AssignSpecialCardAsync(session.Id, f.B.Id, card, Token);
        await scoring.AssignSpecialCardAsync(session.Id, f.A.Id, card, Token);
        Assert.Equal(2, (await f.Scoring.LoadAsync(session.Id, Token))!.SpecialCards.Count);
        await f.Cards.SetArchivedAsync(card, true, Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => scoring.AssignSpecialCardAsync(session.Id, f.A.Id, card, Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => scoring.AssignSpecialCardAsync(session.Id, f.A.Id, Guid.NewGuid(), Token));
        await scoring.RemoveSpecialCardAsync(session.Id, f.A.Id, card, Token);
        Assert.Single((await f.Scoring.LoadAsync(session.Id, Token))!.SpecialCards);
    }

    [Fact]
    public async Task History_ResolvesCurrentNameAndSymbols_RetainsArchivedAssignmentAndAllowsRemoval()
    {
        await using SpecialCardDatabase f = await SpecialCardDatabase.CreateAsync();
        GameSession session = await f.AddSessionAsync(true);
        Guid card = ArkNovaSpecialCardDefaults.SunBear;
        await using (BoardgameTrackerDbContext db = f.CreateDbContext())
        {
            db.Add(new ArkNovaGameSessionSpecialCardAssignment(session.Id, f.A.Id, card));
            await db.SaveChangesAsync(Token);
        }
        await f.Cards.SetArchivedAsync(card, true, Token);
        await f.Cards.UpdateAsync(card, "Bär", ArkNovaCardSymbol.Bear, ArkNovaCardSymbol.Bear, null, Token);
        ArkNovaCorrectionUseCase correction = new(f.Scoring, new SqliteArkNovaCorrectionDataAccess(f));
        ArkNovaCorrectionDraft draft = await correction.BeginAsync(session.Id, Token);
        Assert.Contains(draft.Data.SpecialCards, c => c.SpecialCardId == card);
        ArkNovaSpecialCardData current = draft.Data.CardCatalog.Single(c => c.Id == card);
        Assert.Equal("Bär", current.Name);
        Assert.Equal(new[] { ArkNovaCardSymbol.Bear, ArkNovaCardSymbol.Bear }, current.Symbols);
        Assert.True(current.IsArchived);
        Assert.Throws<InvalidOperationException>(() => draft.SetCard(f.B.Id, card, true));
        await correction.SaveAsync(draft, correction.Prepare(draft).DefaultPlacements, Token);
        draft = await correction.BeginAsync(session.Id, Token);
        Assert.Contains(draft.Data.SpecialCards, c => c.SpecialCardId == card);
        draft.SetCard(f.A.Id, card, false);
        Assert.Throws<InvalidOperationException>(() => draft.SetCard(f.A.Id, card, true));
        await correction.SaveAsync(draft, correction.Prepare(draft).DefaultPlacements, Token);
        Assert.Empty((await f.Scoring.LoadAsync(session.Id, Token))!.SpecialCards);
        Assert.Equal(GameSessionStatus.Completed, (await f.Scoring.LoadAsync(session.Id, Token))!.Status);
    }

    [Fact]
    public async Task Correction_RechecksArchivingBetweenDraftAndSave()
    {
        await using SpecialCardDatabase f = await SpecialCardDatabase.CreateAsync();
        GameSession session = await f.AddSessionAsync(true);
        ArkNovaCorrectionUseCase correction = new(f.Scoring, new SqliteArkNovaCorrectionDataAccess(f));
        ArkNovaCorrectionDraft draft = await correction.BeginAsync(session.Id, Token);
        draft.SetCard(f.A.Id, ArkNovaSpecialCardDefaults.SunBear, true);
        await f.Cards.SetArchivedAsync(ArkNovaSpecialCardDefaults.SunBear, true, Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => correction.SaveAsync(draft, correction.Prepare(draft).DefaultPlacements, Token));
        Assert.Empty((await f.Scoring.LoadAsync(session.Id, Token))!.SpecialCards);
    }

    [Fact]
    public async Task ForeignKey_RestrictsCardDeletion_ButSessionDeletionCascadesAssignments()
    {
        await using SpecialCardDatabase f = await SpecialCardDatabase.CreateAsync();
        GameSession session = await f.AddSessionAsync(true);
        await using (BoardgameTrackerDbContext db = f.CreateDbContext())
        {
            db.Add(new ArkNovaGameSessionSpecialCardAssignment(session.Id, f.A.Id, ArkNovaSpecialCardDefaults.SunBear));
            await db.SaveChangesAsync(Token);
            db.ChangeTracker.Clear(); // Exercise the SQLite FK, not EF's tracked-relationship guard.
            ArkNovaSpecialCard card = await db.ArkNovaSpecialCards.SingleAsync(c => c.Id == ArkNovaSpecialCardDefaults.SunBear, Token);
            db.Remove(card);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Token));
        }
        await new ArkNovaCorrectionUseCase(f.Scoring, new SqliteArkNovaCorrectionDataAccess(f)).DeleteAsync(session.Id, Token);
        await using BoardgameTrackerDbContext check = f.CreateDbContext();
        Assert.Empty(await check.ArkNovaGameSessionSpecialCardAssignments.ToListAsync(Token));
        Assert.Equal(5, await check.ArkNovaSpecialCards.CountAsync(Token));
    }

    [Fact]
    public async Task Upgrade_MapsAllFiveLegacyIntegers_PreservesAssignmentsAndEditedSeedRows()
    {
        await using SpecialCardDatabase f = await SpecialCardDatabase.CreateAsync();
        GameSession session = await f.AddSessionAsync(true);
        await f.MakeLegacyAsync(session.Id, false);
        await new DatabaseInitializer(f).InitializeAsync(Token);
        IReadOnlyList<ArkNovaSpecialCard> defaults = ArkNovaSpecialCardDefaults.Create();
        await using (BoardgameTrackerDbContext db = f.CreateDbContext())
        {
            var assignments = await db.ArkNovaGameSessionSpecialCardAssignments.Where(c => c.GameSessionId == session.Id).ToArrayAsync(Token);
            Assert.Equal(6, assignments.Length);
            Assert.Equal(defaults.Select(c => c.Id).Order(), assignments.Where(c => c.PlayerId == f.A.Id).Select(c => c.SpecialCardId).Order());
            Assert.Contains(assignments, c => c.PlayerId == f.B.Id && c.SpecialCardId == defaults[0].Id);
            Assert.Equal(GameSessionStatus.Completed, (await db.GameSessions.SingleAsync(Token)).Status);
            Assert.Equal(2, await db.ArkNovaGameSessionPlayers.CountAsync(Token));
        }
        await f.Cards.UpdateAsync(defaults[0].Id, "Mein Elefant", null, null, null, Token);
        await f.Cards.SetArchivedAsync(defaults[0].Id, true, Token);
        await new DatabaseInitializer(f).InitializeAsync(Token);
        ArkNovaSpecialCardData edited = (await f.Cards.ListAsync(true, Token)).Single(c => c.Id == defaults[0].Id);
        Assert.Equal("Mein Elefant", edited.Name);
        Assert.Empty(edited.Symbols);
        Assert.True(edited.IsArchived);
        Guid scienceId = await f.Cards.CreateAsync("Forschung nach Upgrade", ArkNovaCardSymbol.Science, null, null, Token);
        Assert.Equal(ArkNovaCardSymbol.Science, (await f.Cards.ListAsync(cancellationToken: Token)).Single(c => c.Id == scienceId).Symbol1);
        await using BoardgameTrackerDbContext verify = f.CreateDbContext();
        Assert.Equal(6, await verify.ArkNovaGameSessionSpecialCardAssignments.CountAsync(Token));
        verify.Remove(await verify.ArkNovaSpecialCards.SingleAsync(c => c.Id == defaults[0].Id, Token));
        await Assert.ThrowsAsync<DbUpdateException>(() => verify.SaveChangesAsync(Token));
    }

    [Fact]
    public async Task Upgrade_ForeignKeyFailureRollsBackDdlAndKeepsLegacyRows()
    {
        await using SpecialCardDatabase f = await SpecialCardDatabase.CreateAsync();
        GameSession session = await f.AddSessionAsync(true);
        await f.MakeLegacyAsync(session.Id, false);
        await using BoardgameTrackerDbContext db = f.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF", Token);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ArkNovaGameSessionSpecialCardAssignments VALUES ({Guid.NewGuid()}, {f.A.Id}, {1})", Token);
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON", Token);
        await Assert.ThrowsAsync<SqliteException>(() => new DatabaseInitializer(f).InitializeAsync(Token));
        Assert.Equal(7, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM ArkNovaGameSessionSpecialCardAssignments WHERE SpecialCard BETWEEN 1 AND 5").SingleAsync(Token));
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE name='ArkNovaSpecialCards'").SingleAsync(Token));
    }

    [Fact]
    public async Task Upgrade_UnknownLegacyValueFailsWithoutDeletingOldData()
    {
        await using SpecialCardDatabase f = await SpecialCardDatabase.CreateAsync();
        GameSession session = await f.AddSessionAsync(true);
        await f.MakeLegacyAsync(session.Id, true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new DatabaseInitializer(f).InitializeAsync(Token));
        await using BoardgameTrackerDbContext db = f.CreateDbContext();
        int count = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM ArkNovaGameSessionSpecialCardAssignments WHERE SpecialCard = 6").SingleAsync(Token);
        Assert.Equal(1, count);
    }
}

internal sealed class SpecialCardDatabase : IDbContextFactory<BoardgameTrackerDbContext>, IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public Player A { get; } = new("Anna");
    public Player B { get; } = new("Ben");
    public Group Group { get; private set; } = null!;
    private Guid _gameId;
    public SqliteArkNovaScoringDataAccess Scoring => new(this);
    public ArkNovaSpecialCardUseCase Cards => new(new SqliteArkNovaSpecialCardDataAccess(this));
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    public BoardgameTrackerDbContext CreateDbContext() => new(new DbContextOptionsBuilder<BoardgameTrackerDbContext>().UseSqlite(_connection).Options);
    public static async Task<SpecialCardDatabase> CreateAsync()
    {
        SpecialCardDatabase f = new();
        await f._connection.OpenAsync(Token);
        await new DatabaseInitializer(f).InitializeAsync(Token);
        await using BoardgameTrackerDbContext db = f.CreateDbContext();
        f._gameId = await db.Games.Select(g => g.Id).SingleAsync(Token);
        f.Group = new("Freitag", false, [f.A.Id, f.B.Id]);
        db.AddRange(f.A, f.B, f.Group);
        await db.SaveChangesAsync(Token);
        return f;
    }
    public async Task<GameSession> AddSessionAsync(bool completed)
    {
        DateTimeOffset start = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        GameSession session = new(_gameId, Group.Id, start, [A.Id, B.Id]);
        session.BeginScoring(start.AddHours(1));
        if (completed)
        {
            session.Players.Single(p => p.PlayerId == A.Id).SetPlacement(1);
            session.Players.Single(p => p.PlayerId == B.Id).SetPlacement(2);
            session.Complete(start.AddHours(2));
        }
        ArkNovaGameSessionPlayer a = new(session.Id, A.Id, PlayerColor.Blue, 1, 2, 1);
        ArkNovaGameSessionPlayer b = new(session.Id, B.Id, PlayerColor.Red, 3, 4, 3);
        a.SetScoreValues(90, 7, 10);
        b.SetScoreValues(70, 7, 9);
        await using BoardgameTrackerDbContext db = CreateDbContext();
        db.AddRange(session, a, b);
        await db.SaveChangesAsync(Token);
        return session;
    }
    public async Task MakeLegacyAsync(Guid gameSessionId, bool unknown)
    {
        await using BoardgameTrackerDbContext db = CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("""
            DROP TABLE ArkNovaGameSessionSpecialCardAssignments;
            DROP TABLE ArkNovaSpecialCards;
            CREATE TABLE ArkNovaGameSessionSpecialCardAssignments (
                GameSessionId TEXT NOT NULL, PlayerId TEXT NOT NULL, SpecialCard INTEGER NOT NULL,
                PRIMARY KEY (GameSessionId, PlayerId, SpecialCard),
                FOREIGN KEY (GameSessionId, PlayerId) REFERENCES GameSessionPlayers (GameSessionId, PlayerId) ON DELETE CASCADE);
            """, Token);
        for (int value = 1; value <= (unknown ? 6 : 5); value++)
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ArkNovaGameSessionSpecialCardAssignments VALUES ({gameSessionId}, {A.Id}, {value})", Token);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ArkNovaGameSessionSpecialCardAssignments VALUES ({gameSessionId}, {B.Id}, {1})", Token);
    }
    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}

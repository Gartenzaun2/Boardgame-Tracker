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
    public async Task Initialization_CreatesCurrentSchemaAndReferenceData()
    {
        await using SpecialCardDatabase fixture = await SpecialCardDatabase.CreateAsync();
        await using BoardgameTrackerDbContext db = fixture.CreateDbContext();
        Assert.Single(await db.Games.ToListAsync(Token));
        Assert.NotEmpty(await db.ArkNovaZooPlans.ToListAsync(Token));
        Assert.Equal(ArkNovaSpecialCardDefaults.Create().Select(card => card.Id).Order(),
            (await db.ArkNovaSpecialCards.Select(card => card.Id).ToArrayAsync(Token)).Order());
        Assert.Empty(await db.ArkNovaGameSessionSpecialCardAssignments.ToListAsync(Token));
    }

    [Fact]
    public async Task Initialization_RepeatedStartPreservesEditedCardsAndSessionAssignments()
    {
        await using SpecialCardDatabase fixture = await SpecialCardDatabase.CreateAsync();
        GameSession session = await fixture.AddSessionAsync(true);
        Guid cardId = ArkNovaSpecialCardDefaults.SunBear;
        await using (BoardgameTrackerDbContext db = fixture.CreateDbContext())
        {
            db.Add(new ArkNovaGameSessionSpecialCardAssignment(session.Id, fixture.A.Id, cardId));
            await db.SaveChangesAsync(Token);
        }
        await fixture.Cards.UpdateAsync(cardId, "Mein Bär", ArkNovaCardSymbol.Science, null, null, Token);
        await fixture.Cards.SetArchivedAsync(cardId, true, Token);
        Guid customId = await fixture.Cards.CreateAsync("Eigene Karte", null, null, null, Token);

        await new DatabaseInitializer(fixture).InitializeAsync(Token);
        await new DatabaseInitializer(fixture).InitializeAsync(Token);

        IReadOnlyList<ArkNovaSpecialCardData> cards = await fixture.Cards.ListAsync(true, Token);
        Assert.Equal(6, cards.Count);
        Assert.Contains(cards, card => card.Id == customId);
        ArkNovaSpecialCardData edited = cards.Single(card => card.Id == cardId);
        Assert.Equal("Mein Bär", edited.Name);
        Assert.Equal(ArkNovaCardSymbol.Science, edited.Symbol1);
        Assert.True(edited.IsArchived);
        ArkNovaScoringGameSessionData data = (await fixture.Scoring.LoadAsync(session.Id, Token))!;
        Assert.Equal(GameSessionStatus.Completed, data.Status);
        Assert.Equal(2, data.Players.Count);
        Assert.Equal(cardId, Assert.Single(data.SpecialCards).SpecialCardId);
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
    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}

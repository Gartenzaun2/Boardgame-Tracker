using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ArkNova.SpecialCards;

public sealed class ArkNovaSpecialCardUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task<Guid> CreateAsync(string name, ArkNovaCardSymbol? symbol1, ArkNovaCardSymbol? symbol2,
        ArkNovaCardSymbol? symbol3, CancellationToken cancellationToken = default)
    {
        ArkNovaSpecialCard card = new(Guid.CreateVersion7(), name, symbol1, symbol2, symbol3);
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        db.ArkNovaSpecialCards.Add(card);
        await db.SaveChangesAsync(cancellationToken);
        return card.Id;
    }
    public async Task UpdateAsync(Guid id, string name, ArkNovaCardSymbol? symbol1, ArkNovaCardSymbol? symbol2,
        ArkNovaCardSymbol? symbol3, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        ArkNovaSpecialCard card = await GetAsync(db, id, cancellationToken);
        card.Update(name, symbol1, symbol2, symbol3);
        await db.SaveChangesAsync(cancellationToken);
    }
    public async Task SetArchivedAsync(Guid id, bool archived, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        ArkNovaSpecialCard card = await GetAsync(db, id, cancellationToken);
        card.SetArchived(archived);
        await db.SaveChangesAsync(cancellationToken);
    }
    private static async Task<ArkNovaSpecialCard> GetAsync(BoardgameTrackerDbContext db, Guid id, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        return await db.ArkNovaSpecialCards.SingleOrDefaultAsync(card => card.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("The special card does not exist.");
    }

    public async Task<IReadOnlyList<ArkNovaSpecialCardData>> ListAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.ArkNovaSpecialCards.AsNoTracking().Where(c => includeArchived || !c.IsArchived)
            .OrderBy(c => c.Name).ThenBy(c => c.Id)
            .Select(c => new ArkNovaSpecialCardData(c.Id, c.Name, c.Symbol1, c.Symbol2, c.Symbol3, c.IsArchived))
            .ToArrayAsync(cancellationToken);
    }
}

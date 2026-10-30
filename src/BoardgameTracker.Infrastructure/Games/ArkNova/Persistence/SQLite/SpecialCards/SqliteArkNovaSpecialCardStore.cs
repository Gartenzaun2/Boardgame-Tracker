using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.SpecialCards;

public sealed class SqliteArkNovaSpecialCardStore(IDbContextFactory<BoardgameTrackerDbContext> factory) : IArkNovaSpecialCardStore
{
    public async Task<IReadOnlyList<ArkNovaSpecialCardData>> ListAsync(bool includeArchived, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.ArkNovaSpecialCards.AsNoTracking().Where(c => includeArchived || !c.IsArchived)
            .OrderBy(c => c.Name).ThenBy(c => c.Id)
            .Select(c => new ArkNovaSpecialCardData(c.Id, c.Name, c.Symbol1, c.Symbol2, c.Symbol3, c.IsArchived))
            .ToArrayAsync(cancellationToken);
    }
    public async Task<ArkNovaSpecialCard?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.ArkNovaSpecialCards.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
    public async Task SaveAsync(ArkNovaSpecialCard card, bool isNew, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        if (isNew)
            db.ArkNovaSpecialCards.Add(card);
        else
            db.ArkNovaSpecialCards.Update(card);
        await db.SaveChangesAsync(cancellationToken);
    }
}

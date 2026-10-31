using BoardgameTracker.Application.Games.ArkNova.Content;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ArkNova.SpecialCards;

public sealed class ArkNovaSpecialCardUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory, ArkNovaContent content)
{
    public async Task<Guid> CreateAsync(string name, ArkNovaCardSymbol? symbol1, ArkNovaCardSymbol? symbol2,
        ArkNovaCardSymbol? symbol3, CancellationToken cancellationToken = default)
    {
        ArkNovaSpecialCard card = new(Guid.CreateVersion7(), symbol1, symbol2, symbol3);
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        db.ArkNovaSpecialCards.Add(card);
        db.ArkNovaUserContent.Add(new(ArkNovaUserContent.SpecialCard, card.Id.ToString(), name));
        await db.SaveChangesAsync(cancellationToken);
        return card.Id;
    }
    public async Task UpdateAsync(Guid id, string name, ArkNovaCardSymbol? symbol1, ArkNovaCardSymbol? symbol2,
        ArkNovaCardSymbol? symbol3, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        ArkNovaSpecialCard card = await GetAsync(db, id, cancellationToken);
        card.UpdateSymbols(symbol1, symbol2, symbol3);
        string objectId = id.ToString();
        ArkNovaUserContent? user = await db.ArkNovaUserContent.SingleOrDefaultAsync(
            value => value.ResourceType == ArkNovaUserContent.SpecialCard && value.ObjectId == objectId, cancellationToken);
        // Saving symbols alone must not freeze a built-in name into user content.
        if (!string.Equals(name.Trim(), content.ResolveName(ArkNovaUserContent.SpecialCard, objectId, user?.Name), StringComparison.Ordinal))
        {
            if (user is null)
                db.ArkNovaUserContent.Add(new(ArkNovaUserContent.SpecialCard, objectId, name));
            else
                user.SetName(name);
        }
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
        return await content.ReadCardCatalogAsync(db, includeArchived, cancellationToken);
    }
}

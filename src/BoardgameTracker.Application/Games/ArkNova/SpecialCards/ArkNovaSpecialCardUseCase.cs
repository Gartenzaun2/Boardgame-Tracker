using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Games.ArkNova.SpecialCards;

public sealed class ArkNovaSpecialCardUseCase(IArkNovaSpecialCardDataAccess dataAccess)
{
    public Task<IReadOnlyList<ArkNovaSpecialCardData>> ListAsync(bool includeArchived = false, CancellationToken cancellationToken = default) =>
        dataAccess.ListAsync(includeArchived, cancellationToken);

    public async Task<Guid> CreateAsync(string name, ArkNovaCardSymbol? symbol1, ArkNovaCardSymbol? symbol2,
        ArkNovaCardSymbol? symbol3, CancellationToken cancellationToken = default)
    {
        ArkNovaSpecialCard card = new(Guid.NewGuid(), name, symbol1, symbol2, symbol3);
        await dataAccess.SaveAsync(card, true, cancellationToken);
        return card.Id;
    }
    public async Task UpdateAsync(Guid id, string name, ArkNovaCardSymbol? symbol1, ArkNovaCardSymbol? symbol2,
        ArkNovaCardSymbol? symbol3, CancellationToken cancellationToken = default)
    {
        ArkNovaSpecialCard card = await GetAsync(id, cancellationToken);
        card.Update(name, symbol1, symbol2, symbol3);
        await dataAccess.SaveAsync(card, false, cancellationToken);
    }
    public async Task SetArchivedAsync(Guid id, bool archived, CancellationToken cancellationToken = default)
    {
        ArkNovaSpecialCard card = await GetAsync(id, cancellationToken);
        card.SetArchived(archived);
        await dataAccess.SaveAsync(card, false, cancellationToken);
    }
    private async Task<ArkNovaSpecialCard> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        return await dataAccess.FindAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("The special card does not exist.");
    }
}

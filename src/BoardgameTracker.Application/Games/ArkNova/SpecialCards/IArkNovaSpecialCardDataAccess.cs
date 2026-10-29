using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Games.ArkNova.SpecialCards;

public interface IArkNovaSpecialCardDataAccess
{
    Task<IReadOnlyList<ArkNovaSpecialCardData>> ListAsync(bool includeArchived, CancellationToken cancellationToken = default);
    Task<ArkNovaSpecialCard?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(ArkNovaSpecialCard card, bool isNew, CancellationToken cancellationToken = default);
}

public sealed record ArkNovaSpecialCardData(Guid Id, string Name, ArkNovaCardSymbol? Symbol1,
    ArkNovaCardSymbol? Symbol2, ArkNovaCardSymbol? Symbol3, bool IsArchived)
{
    public IReadOnlyList<ArkNovaCardSymbol> Symbols => new[] { Symbol1, Symbol2, Symbol3 }.OfType<ArkNovaCardSymbol>().ToArray();
}

namespace BoardgameTracker.Core.Games.ArkNova;

public sealed class ArkNovaSpecialCard
{
    private ArkNovaSpecialCard() { }
    public ArkNovaSpecialCard(Guid id, ArkNovaCardSymbol? symbol1 = null,
        ArkNovaCardSymbol? symbol2 = null, ArkNovaCardSymbol? symbol3 = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        Id = id;
        UpdateSymbols(symbol1, symbol2, symbol3);
    }
    public Guid Id { get; private set; }
    public ArkNovaCardSymbol? Symbol1 { get; private set; }
    public ArkNovaCardSymbol? Symbol2 { get; private set; }
    public ArkNovaCardSymbol? Symbol3 { get; private set; }
    public bool IsArchived { get; private set; }

    public void UpdateSymbols(ArkNovaCardSymbol? symbol1, ArkNovaCardSymbol? symbol2, ArkNovaCardSymbol? symbol3)
    {
        if ((symbol1 is null && symbol2 is not null) || (symbol2 is null && symbol3 is not null))
            throw new ArgumentException("Symbols must be contiguous and ordered.");
        foreach (ArkNovaCardSymbol? symbol in new[] { symbol1, symbol2, symbol3 })
        {
            if (symbol is ArkNovaCardSymbol value && !Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(symbol1));
        }
        Symbol1 = symbol1;
        Symbol2 = symbol2;
        Symbol3 = symbol3;
    }
    public void SetArchived(bool archived) => IsArchived = archived;
}

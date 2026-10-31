using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;

namespace BoardgameTracker.Core.Tests.Games.ArkNova;

public sealed class ArkNovaSpecialCardTests
{
    [Fact]
    public void Card_PreservesIdentityOrderedRepeatedSymbolsAndArchiveState()
    {
        Guid id = Guid.NewGuid();
        ArkNovaSpecialCard card = new(id, ArkNovaCardSymbol.Africa, ArkNovaCardSymbol.Africa, ArkNovaCardSymbol.Herbivore);
        Assert.Equal(ArkNovaCardSymbol.Africa, card.Symbol1);
        Assert.Equal(ArkNovaCardSymbol.Africa, card.Symbol2);
        card.SetArchived(true);
        card.UpdateSymbols(ArkNovaCardSymbol.Predator, null, null);
        Assert.Equal(id, card.Id);
        Assert.True(card.IsArchived);
        Assert.Null(card.Symbol2);
        card.SetArchived(false);
        Assert.False(card.IsArchived);
    }
    [Fact]
    public void Card_RequiresNoNameAndAllowsZeroSymbols()
    {
        ArkNovaSpecialCard card = new(Guid.NewGuid());
        Assert.Null(card.Symbol1);
        Assert.Null(card.Symbol2);
        Assert.Null(card.Symbol3);
    }
    [Fact]
    public void Science_IsTheFourteenthStableSymbol()
    {
        ArkNovaSpecialCard card = new(Guid.NewGuid(), ArkNovaCardSymbol.Science);
        Assert.Equal(14, (int)card.Symbol1!.Value);
    }
    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(0, 0, 1)]
    [InlineData(15, 0, 0)]
    [InlineData(-1, 0, 0)]
    public void InvalidSymbolSequences_AreRejected(int first, int second, int third)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ArkNovaSpecialCard(Guid.NewGuid(),
            first == 0 ? null : (ArkNovaCardSymbol)first,
            second == 0 ? null : (ArkNovaCardSymbol)second,
            third == 0 ? null : (ArkNovaCardSymbol)third));
    }
    [Fact]
    public void InvalidIds_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArkNovaSpecialCard(Guid.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArkNovaGameSessionSpecialCardAssignment(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));
    }
}

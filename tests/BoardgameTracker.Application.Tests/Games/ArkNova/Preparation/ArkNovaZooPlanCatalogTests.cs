using BoardgameTracker.Application.Games.ArkNova.Preparation;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.Preparation;

public sealed class ArkNovaZooPlanCatalogTests
{
    [Fact]
    public void Catalog_ContainsFourteenUniquePlans()
    {
        Assert.Equal(14, ArkNovaZooPlanCatalog.All.Count);
        Assert.Equal(14, ArkNovaZooPlanCatalog.All.Select(plan => plan.Number).Distinct().Count());
    }

    [Fact]
    public void Catalog_UsesExpectedPlanNumbers()
    {
        Assert.Equal(Enumerable.Range(1, 14), ArkNovaZooPlanCatalog.All.Select(plan => plan.Number));
    }
}

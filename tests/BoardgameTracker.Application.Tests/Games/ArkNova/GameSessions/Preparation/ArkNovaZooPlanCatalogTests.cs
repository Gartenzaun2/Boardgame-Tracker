using BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;

namespace BoardgameTracker.Application.Tests.Games.ArkNova.GameSessions.Preparation;

public sealed class ArkNovaZooPlanCatalogTests
{
    [Fact]
    public void Catalog_ContainsSeventeenUniquePlans()
    {
        Assert.Equal(17, ArkNovaZooPlanCatalog.All.Count);
        Assert.Equal(17, ArkNovaZooPlanCatalog.All.Select(plan => plan.Number).Distinct().Count());
    }

    [Fact]
    public void Catalog_UsesExpectedPlanNumbers()
    {
        Assert.Equal(Enumerable.Range(1, 17), ArkNovaZooPlanCatalog.All.Select(plan => plan.Number));
    }
    [Fact]
    public void DrawPool_ContainsOnlyRegularPlans()
    {
        Assert.Equal(Enumerable.Range(1, 14), ArkNovaZooPlanCatalog.DrawPool.Select(plan => plan.Number));
    }

}

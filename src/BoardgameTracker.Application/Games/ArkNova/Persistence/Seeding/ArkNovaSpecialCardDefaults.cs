using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Games.ArkNova.Persistence.Seeding;

public static class ArkNovaSpecialCardDefaults
{
    public static readonly Guid AfricanElephant = new("a11c0001-0000-4000-8000-000000000001");
    public static readonly Guid AsianElephant = new("a11c0001-0000-4000-8000-000000000002");
    public static readonly Guid BaldEagle = new("a11c0001-0000-4000-8000-000000000003");
    public static readonly Guid GoldenEagle = new("a11c0001-0000-4000-8000-000000000004");
    public static readonly Guid SunBear = new("a11c0001-0000-4000-8000-000000000005");

    public static IReadOnlyList<ArkNovaSpecialCard> Create() =>
    [
        new(AfricanElephant, ArkNovaCardSymbol.Africa, ArkNovaCardSymbol.Africa, ArkNovaCardSymbol.Herbivore),
        new(AsianElephant, ArkNovaCardSymbol.Asia, ArkNovaCardSymbol.Asia, ArkNovaCardSymbol.Herbivore),
        new(BaldEagle, ArkNovaCardSymbol.America, ArkNovaCardSymbol.Bird),
        new(GoldenEagle, ArkNovaCardSymbol.Europe, ArkNovaCardSymbol.Bird),
        new(SunBear, ArkNovaCardSymbol.Asia, ArkNovaCardSymbol.Bear, ArkNovaCardSymbol.Predator)
    ];
}

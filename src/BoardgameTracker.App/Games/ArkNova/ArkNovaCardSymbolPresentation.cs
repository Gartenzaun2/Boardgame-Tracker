using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.App.Games.ArkNova;

internal static class ArkNovaCardSymbolPresentation
{
    public static string Image(ArkNovaCardSymbol symbol) => symbol switch
    {
        ArkNovaCardSymbol.Africa => "ark_symbol_africa.png",
        ArkNovaCardSymbol.America => "ark_symbol_america.png",
        ArkNovaCardSymbol.Asia => "ark_symbol_asia.png",
        ArkNovaCardSymbol.Australia => "ark_symbol_australia.png",
        ArkNovaCardSymbol.Europe => "ark_symbol_europe.png",
        ArkNovaCardSymbol.Bear => "ark_symbol_bear.png",
        ArkNovaCardSymbol.Bird => "ark_symbol_bird.png",
        ArkNovaCardSymbol.Herbivore => "ark_symbol_herbivore.png",
        ArkNovaCardSymbol.PettingAnimal => "ark_symbol_pettinganimal.png",
        ArkNovaCardSymbol.Predator => "ark_symbol_predator.png",
        ArkNovaCardSymbol.Primate => "ark_symbol_primate.png",
        ArkNovaCardSymbol.Reptile => "ark_symbol_reptile.png",
        ArkNovaCardSymbol.SeaAnimal => "ark_symbol_seaanimal.png",
        ArkNovaCardSymbol.Science => "ark_symbol_science.png",
        _ => throw new ArgumentOutOfRangeException(nameof(symbol))
    };
    public static string Name(ArkNovaCardSymbol symbol) => symbol switch
    {
        ArkNovaCardSymbol.Africa => AppResources.CardSymbolAfrica,
        ArkNovaCardSymbol.America => AppResources.CardSymbolAmerica,
        ArkNovaCardSymbol.Asia => AppResources.CardSymbolAsia,
        ArkNovaCardSymbol.Australia => AppResources.CardSymbolAustralia,
        ArkNovaCardSymbol.Europe => AppResources.CardSymbolEurope,
        ArkNovaCardSymbol.Bear => AppResources.CardSymbolBear,
        ArkNovaCardSymbol.Bird => AppResources.CardSymbolBird,
        ArkNovaCardSymbol.Herbivore => AppResources.CardSymbolHerbivore,
        ArkNovaCardSymbol.PettingAnimal => AppResources.CardSymbolPettingAnimal,
        ArkNovaCardSymbol.Predator => AppResources.CardSymbolPredator,
        ArkNovaCardSymbol.Primate => AppResources.CardSymbolPrimate,
        ArkNovaCardSymbol.Reptile => AppResources.CardSymbolReptile,
        ArkNovaCardSymbol.SeaAnimal => AppResources.CardSymbolSeaAnimal,
        ArkNovaCardSymbol.Science => AppResources.CardSymbolScience,
        _ => throw new ArgumentOutOfRangeException(nameof(symbol))
    };
}

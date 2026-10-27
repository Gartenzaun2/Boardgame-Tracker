using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.App.Games.ArkNova;

/// <summary>
/// Maps the stable Ark Nova special-card enum values to localized presentation text.
/// </summary>
internal static class ArkNovaSpecialCardPresentation
{
    public static string GetName(ArkNovaSpecialCard card) => card switch
    {
        ArkNovaSpecialCard.AfricanElephant => AppResources.ArkNovaSpecialCardAfricanElephantName,
        ArkNovaSpecialCard.AsianElephant => AppResources.ArkNovaSpecialCardAsianElephantName,
        ArkNovaSpecialCard.BaldEagle => AppResources.ArkNovaSpecialCardBaldEagleName,
        ArkNovaSpecialCard.GoldenEagle => AppResources.ArkNovaSpecialCardGoldenEagleName,
        ArkNovaSpecialCard.SunBear => AppResources.ArkNovaSpecialCardSunBearName,
        _ => throw new ArgumentOutOfRangeException(nameof(card))
    };

    public static string GetSymbols(ArkNovaSpecialCard card) => card switch
    {
        ArkNovaSpecialCard.AfricanElephant => AppResources.ArkNovaSpecialCardAfricanElephantSymbols,
        ArkNovaSpecialCard.AsianElephant => AppResources.ArkNovaSpecialCardAsianElephantSymbols,
        ArkNovaSpecialCard.BaldEagle => AppResources.ArkNovaSpecialCardBaldEagleSymbols,
        ArkNovaSpecialCard.GoldenEagle => AppResources.ArkNovaSpecialCardGoldenEagleSymbols,
        ArkNovaSpecialCard.SunBear => AppResources.ArkNovaSpecialCardSunBearSymbols,
        _ => throw new ArgumentOutOfRangeException(nameof(card))
    };
}

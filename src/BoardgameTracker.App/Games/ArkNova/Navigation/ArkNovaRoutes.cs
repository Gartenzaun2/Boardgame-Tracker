using BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Games.ArkNova.Features.Settings;

namespace BoardgameTracker.App.Games.ArkNova.Navigation;

public static class ArkNovaRoutes
{
    public static void Register()
    {
        Routing.RegisterRoute(nameof(ArkNovaPreparationPage), typeof(ArkNovaPreparationPage));
        Routing.RegisterRoute(nameof(ArkNovaRunningPage), typeof(ArkNovaRunningPage));
        Routing.RegisterRoute(nameof(ArkNovaScoringPage), typeof(ArkNovaScoringPage));
        Routing.RegisterRoute(nameof(ArkNovaPlayerScoringPage), typeof(ArkNovaPlayerScoringPage));
        Routing.RegisterRoute(nameof(ArkNovaTieResolutionPage), typeof(ArkNovaTieResolutionPage));
        Routing.RegisterRoute(nameof(ArkNovaResultPage), typeof(ArkNovaResultPage));
        Routing.RegisterRoute(nameof(ArkNovaSpecialCardsPage), typeof(ArkNovaSpecialCardsPage));
        Routing.RegisterRoute(nameof(ArkNovaSpecialCardEditorPage), typeof(ArkNovaSpecialCardEditorPage));
        Routing.RegisterRoute(nameof(ArkNovaSettingsPage), typeof(ArkNovaSettingsPage));
    }
}

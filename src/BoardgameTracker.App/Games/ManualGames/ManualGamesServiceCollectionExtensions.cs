using BoardgameTracker.App.Games.ManualGames.Features.Settings;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Scoring;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Result;
using BoardgameTracker.Application.Games.ManualGames.GameSessions;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Preparation;
using BoardgameTracker.App.Games.ManualGames.Features.GameSessions.Running;
using BoardgameTracker.Application.Games.ManualGames;
using Microsoft.Extensions.DependencyInjection;

namespace BoardgameTracker.App.Games.ManualGames;

public static class ManualGamesServiceCollectionExtensions
{
    public static IServiceCollection AddManualGames(this IServiceCollection services)
    {
        services.AddTransient<ManualGameUseCase>();
        services.AddTransient<ManualPreparationQuery>();
        services.AddTransient<StartManualGameSessionUseCase>();
        services.AddTransient<ManualScoringQuery>();
        services.AddTransient<ManualScoringUseCase>();
        services.AddTransient<ManualHistoryCorrectionUseCase>();
        Routing.RegisterRoute(ManualGameSessionNavigationHandler.CorrectionRoute, typeof(ManualScoringPage));
        services.AddTransient<CompleteManualGameSessionUseCase>();
        services.AddTransient<ManualScoringViewModel>();
        services.AddTransient<ManualScoringPage>();
        services.AddTransient<ManualResultViewModel>();
        services.AddTransient<ManualResultPage>();
        services.AddTransient<ManualGameSessionNavigationHandler>();
        Routing.RegisterRoute(ManualGameSessionNavigationHandler.PointsRoute, typeof(ManualScoringPage));
        Routing.RegisterRoute(ManualGameSessionNavigationHandler.DecisionRoute, typeof(ManualScoringPage));
        Routing.RegisterRoute(nameof(ManualResultPage), typeof(ManualResultPage));
        services.AddTransient<ManualRunningGameSessionQuery>();
        services.AddTransient<ManualRunningGameSessionUseCase>();
        services.AddTransient<ManualPreparationViewModel>();
        services.AddTransient<ManualPreparationPage>();
        services.AddTransient<ManualRunningViewModel>();
        services.AddTransient<ManualRunningPage>();
        services.AddTransient<IGameSessionNavigationHandler, ManualGameSessionNavigationHandler>();
        services.AddSingleton<IGameSessionResumeHandler, ManualGameSessionNavigationHandler>();
        Routing.RegisterRoute(nameof(ManualPreparationPage), typeof(ManualPreparationPage));
        Routing.RegisterRoute(nameof(ManualRunningPage), typeof(ManualRunningPage));
        services.AddTransient<ManualGameEditorViewModel>();
        services.AddTransient<ManualGameEditorPage>();
        services.AddSingleton<ManualGamesNavigation>();
        services.AddSingleton<IGameSettingsNavigation>(provider => provider.GetRequiredService<ManualGamesNavigation>());
        services.AddSingleton<IGameCreationNavigation>(provider => provider.GetRequiredService<ManualGamesNavigation>());
        Routing.RegisterRoute(nameof(ManualGameEditorPage), typeof(ManualGameEditorPage));
        return services;
    }
}

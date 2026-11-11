using BoardgameTracker.App.Games.ManualGames.Features.Settings;
using BoardgameTracker.App.Navigation;
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

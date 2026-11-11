using BoardgameTracker.App.Games.ManualGames.Features.Settings;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.Games.ManualGames;
using Microsoft.Extensions.DependencyInjection;

namespace BoardgameTracker.App.Games.ManualGames;

public static class ManualGamesServiceCollectionExtensions
{
    public static IServiceCollection AddManualGames(this IServiceCollection services)
    {
        services.AddTransient<ManualGameUseCase>();
        services.AddTransient<ManualGameEditorViewModel>();
        services.AddTransient<ManualGameEditorPage>();
        services.AddSingleton<ManualGamesNavigation>();
        services.AddSingleton<IGameSettingsNavigation>(provider => provider.GetRequiredService<ManualGamesNavigation>());
        services.AddSingleton<IGameCreationNavigation>(provider => provider.GetRequiredService<ManualGamesNavigation>());
        Routing.RegisterRoute(nameof(ManualGameEditorPage), typeof(ManualGameEditorPage));
        return services;
    }
}

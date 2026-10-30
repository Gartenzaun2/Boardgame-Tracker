using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Games.ArkNova.Navigation;
using BoardgameTracker.App.Features.Groups.Colors;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Games.ArkNova.Features.Settings;
using BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.App.Services;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Running;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.StartGameSession;
using BoardgameTracker.Application.Games.ArkNova.Settings;
using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.Seeding;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;
using Microsoft.Extensions.DependencyInjection;

namespace BoardgameTracker.App.Games.ArkNova;

public static class ArkNovaServiceCollectionExtensions
{
    public static IServiceCollection AddArkNova(this IServiceCollection services)
    {
        services.AddTransient<IGameHistoryHandler, ArkNovaGameHistoryHandler>();
        services.AddSingleton<IDatabaseInitializationStep, ArkNovaReferenceDataSeeder>();
        services.AddSingleton(ArkNovaPlayerColors.Rules);
        services.AddSingleton<IGroupColorPersistence, ArkNovaGroupColorPersistence>();
        services.AddSingleton(new GameColorPresentation(ArkNovaGame.Key, () => AppResources.ArkNova));
        services.AddTransient<IArkNovaPreparationQuery, SqliteArkNovaPreparationQuery>();
        services.AddTransient<IStartArkNovaGameSessionDataAccess, SqliteStartArkNovaGameSessionDataAccess>();
        services.AddTransient<StartArkNovaGameSessionUseCase>();
        services.AddTransient<IArkNovaRunningGameSessionQuery, SqliteArkNovaRunningGameSessionQuery>();
        services.AddTransient<IArkNovaScoringDataAccess, SqliteArkNovaScoringDataAccess>();
        services.AddTransient<ArkNovaScoringUseCase>();
        services.AddSingleton<IArkNovaSettingsStore, MauiArkNovaSettingsStore>();
        services.AddTransient<IArkNovaCorrectionDataAccess, SqliteArkNovaCorrectionDataAccess>();
        services.AddTransient<ArkNovaCorrectionUseCase>();
        services.AddTransient<ArkNovaPreparationViewModel>();
        services.AddTransient<ArkNovaPreparationPage>();
        services.AddTransient<ArkNovaRunningViewModel>();
        services.AddTransient<ArkNovaRunningPage>();
        services.AddTransient<ArkNovaScoringViewModel>();
        services.AddTransient<ArkNovaScoringPage>();
        services.AddTransient<ArkNovaPlayerScoringViewModel>();
        services.AddTransient<ArkNovaPlayerScoringPage>();
        services.AddTransient<ArkNovaTieResolutionViewModel>();
        services.AddTransient<ArkNovaTieResolutionPage>();
        services.AddTransient<ArkNovaResultViewModel>();
        services.AddTransient<ArkNovaResultPage>();
        services.AddTransient<IArkNovaSpecialCardDataAccess, SqliteArkNovaSpecialCardDataAccess>();
        services.AddTransient<ArkNovaSpecialCardUseCase>();
        services.AddTransient<ArkNovaSpecialCardsViewModel>();
        services.AddTransient<ArkNovaSpecialCardsPage>();
        services.AddTransient<ArkNovaSpecialCardEditorViewModel>();
        services.AddTransient<ArkNovaSpecialCardEditorPage>();
        services.AddTransient<ArkNovaSettingsViewModel>();
        services.AddTransient<ArkNovaSettingsPage>();

        services.AddSingleton<IGameSessionResumeHandler, ArkNovaGameSessionResumeHandler>();
        ArkNovaRoutes.Register();

        return services;
    }
}

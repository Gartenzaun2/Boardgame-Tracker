using BoardgameTracker.Application.Games.ArkNova.Content;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;
using BoardgameTracker.Application.Games.ArkNova.Persistence;

using BoardgameTracker.Application.Games.ArkNova.GameSessions;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;

using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Application.Games.ArkNova.History.Transfer;
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
using BoardgameTracker.Application.Games.ArkNova.GameSessions.History;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.Settings;
using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Application.Games.ArkNova.Persistence.Seeding;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Groups.Colors;
using Microsoft.Extensions.DependencyInjection;
using BoardgameTracker.App.Games.ArkNova.PhotoRecognition;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring.PhotoRecognition;

namespace BoardgameTracker.App.Games.ArkNova;

public static class ArkNovaServiceCollectionExtensions
{
    public static IServiceCollection AddArkNova(this IServiceCollection services)
    {
        services.AddSingleton(ArkNovaContentCatalog.LoadBuiltIn());
        services.AddSingleton<ArkNovaContent>();
        services.AddSingleton<IGameTransferSchema, ArkNovaTransferSchema>();
        services.AddSingleton<IGameTransferPersistence, ArkNovaTransferPersistence>();
        services.AddTransient<IGameSessionNavigationHandler, ArkNovaGameSessionNavigationHandler>();
        services.AddSingleton<IDatabaseInitializationStep, ArkNovaReferenceDataSeeder>();
        services.AddSingleton(ArkNovaPlayerColors.Rules);
        services.AddSingleton<IGroupColorPersistence, ArkNovaGroupColorPersistence>();
        services.AddSingleton(new GameColorPresentation(ArkNovaGame.Key, () => AppResources.ArkNova));
        services.AddTransient<ArkNovaPreparationQuery>();
        services.AddTransient<StartArkNovaGameSessionUseCase>();
        services.AddTransient<ArkNovaRunningGameSessionQuery>();
        services.AddTransient<ArkNovaGameSessionResultQuery>();
        services.AddTransient<ArkNovaScoringUseCase>();
        services.AddSingleton<IArkNovaVisionEngine>(_ => new NativeArkNovaVisionEngine(async name =>
        {
            using Stream input = await FileSystem.OpenAppPackageFileAsync(name);
            using MemoryStream output = new();
            await input.CopyToAsync(output);
            return output.ToArray();
        }));
        services.AddSingleton<IMediaPicker>(MediaPicker.Default);
        services.AddSingleton<IArkNovaPhotoInput, MauiArkNovaPhotoInput>();
        services.AddTransient<ArkNovaPhotoRecognition>();
        services.AddTransient<ArkNovaPhotoReviewFlow>();
        services.AddSingleton<IArkNovaSettingsStore, MauiArkNovaSettingsStore>();
        services.AddTransient<ArkNovaHistoryCorrectionUseCase>();
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
        services.AddTransient<ArkNovaSpecialCardUseCase>();
        services.AddTransient<ArkNovaSpecialCardsViewModel>();
        services.AddTransient<ArkNovaSpecialCardsPage>();
        services.AddTransient<ArkNovaSpecialCardEditorViewModel>();
        services.AddTransient<ArkNovaSpecialCardEditorPage>();
        services.AddTransient<ArkNovaSettingsViewModel>();
        services.AddTransient<ArkNovaSettingsPage>();

        services.AddSingleton<IGameSessionResumeHandler, ArkNovaGameSessionNavigationHandler>();
        ArkNovaRoutes.Register();

        return services;
    }
}

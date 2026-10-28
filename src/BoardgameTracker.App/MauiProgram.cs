using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;
using BoardgameTracker.App.Games.ArkNova.Features.Settings;
using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Features.History;
using BoardgameTracker.App.Features.Settings.AppSettings;
using BoardgameTracker.App.Services;
using BoardgameTracker.Application.Groups.CreateGroup;
using BoardgameTracker.Application.Groups.ListGroups;
using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.StartGameSession;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Running;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Application.Games.ArkNova.Settings;
using BoardgameTracker.Application.GameSessions.Running;
using BoardgameTracker.Application.GameSessions.Resume;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;
using BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite.GameSessions;
using Microsoft.Extensions.DependencyInjection;

namespace BoardgameTracker.App;

/// <summary>
/// Composition root for MAUI, EF Core, Application use cases, persistence ports, pages and view models.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();
        builder.Services.AddSingleton<BoardgameTracker.App.Theming.ThemeManager>();
        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            handlers.AddHandler<Shell, BoardgameTracker.App.Platforms.Android.ThemeShellRenderer>();
#elif IOS
            handlers.AddHandler<Shell, BoardgameTracker.App.Platforms.iOS.ThemeShellRenderer>();
#endif
        });

        string databasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "boardgame-tracker.db");

        builder.Services.AddDbContextFactory<BoardgameTrackerDbContext>(
            options => BoardgameTrackerDbContextOptions.Configure(
                options,
                $"Data Source={databasePath}"));

        builder.Services.AddSingleton<DatabaseInitializer>();

        builder.Services.AddSingleton<IListGroupsQuery, SqliteListGroupsQuery>();
        builder.Services.AddTransient<ICreateGroupDataAccess, SqliteCreateGroupDataAccess>();
        builder.Services.AddTransient<CreateGroupUseCase>();
        builder.Services.AddTransient<IEditGroupDataAccess, SqliteEditGroupDataAccess>();
        builder.Services.AddTransient<EditGroupUseCase>();
        builder.Services.AddTransient<IArkNovaPreparationQuery, SqliteArkNovaPreparationQuery>();
        builder.Services.AddTransient<IStartArkNovaGameSessionDataAccess, SqliteStartArkNovaGameSessionDataAccess>();
        builder.Services.AddTransient<StartArkNovaGameSessionUseCase>();
        builder.Services.AddTransient<IArkNovaRunningGameSessionQuery, SqliteArkNovaRunningGameSessionQuery>();
        builder.Services.AddTransient<IRunningGameSessionDataAccess, SqliteRunningGameSessionDataAccess>();
        builder.Services.AddTransient<RunningGameSessionUseCase>();
        builder.Services.AddTransient<IOpenGameSessionQuery, SqliteOpenGameSessionQuery>();
        builder.Services.AddTransient<IArkNovaScoringDataAccess, SqliteArkNovaScoringDataAccess>();
        builder.Services.AddTransient<ArkNovaScoringUseCase>();
        builder.Services.AddSingleton<IArkNovaSettingsStore, MauiArkNovaSettingsStore>();
        builder.Services.AddSingleton<IAppNavigationSettingsStore, MauiAppNavigationSettingsStore>();

        builder.Services.AddSingleton<GroupListViewModel>();
        builder.Services.AddSingleton<GroupListPage>();
        builder.Services.AddSingleton<GamesPage>();
        builder.Services.AddSingleton<HistoryPage>();
        builder.Services.AddSingleton<BoardgameTracker.Application.History.IHistoryQuery, SqliteHistoryQuery>();
        builder.Services.AddTransient<BoardgameTracker.Application.Games.ArkNova.GameSessions.History.IArkNovaCorrectionDataAccess, SqliteArkNovaCorrectionDataAccess>();
        builder.Services.AddTransient<BoardgameTracker.Application.Games.ArkNova.GameSessions.History.ArkNovaCorrectionUseCase>();
        builder.Services.AddSingleton<AppSettingsPage>();
        builder.Services.AddTransient<CreateGroupViewModel>();
        builder.Services.AddTransient<CreateGroupPage>();
        builder.Services.AddTransient<EditGroupViewModel>();
        builder.Services.AddTransient<EditGroupPage>();
        builder.Services.AddTransient<ArkNovaPreparationViewModel>();
        builder.Services.AddTransient<ArkNovaPreparationPage>();
        builder.Services.AddTransient<ArkNovaRunningViewModel>();
        builder.Services.AddTransient<ArkNovaRunningPage>();
        builder.Services.AddTransient<ArkNovaScoringViewModel>();
        builder.Services.AddTransient<ArkNovaScoringPage>();
        builder.Services.AddTransient<ArkNovaPlayerScoringViewModel>();
        builder.Services.AddTransient<ArkNovaPlayerScoringPage>();
        builder.Services.AddTransient<ArkNovaTieResolutionViewModel>();
        builder.Services.AddTransient<ArkNovaTieResolutionPage>();
        builder.Services.AddTransient<ArkNovaResultViewModel>();
        builder.Services.AddTransient<ArkNovaResultPage>();
        builder.Services.AddTransient<ArkNovaSettingsViewModel>();
        builder.Services.AddTransient<ArkNovaSettingsPage>();

        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}

using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.GameSessions.Preparation;
using BoardgameTracker.App.Features.GameSessions.Running;
using BoardgameTracker.App.Features.GameSessions.Scoring;
using BoardgameTracker.App.Features.GameSessions.Result;
using BoardgameTracker.App.Features.Games.ArkNova.Settings;
using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Features.History;
using BoardgameTracker.App.Features.Settings.AppSettings;
using BoardgameTracker.App.Services;
using BoardgameTracker.Application.Groups.CreateGroup;
using BoardgameTracker.Application.Groups.ListGroups;
using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Application.Games.ArkNova.Preparation;
using BoardgameTracker.Application.Games.ArkNova.StartSession;
using BoardgameTracker.Application.Games.ArkNova.Running;
using BoardgameTracker.Application.Games.ArkNova.Scoring;
using BoardgameTracker.Application.Games.ArkNova.Settings;
using BoardgameTracker.Application.GameSessions.Running;
using BoardgameTracker.Application.GameSessions.Resume;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Games.ArkNova;
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
        builder.Services.AddTransient<IStartArkNovaSessionDataAccess, SqliteStartArkNovaSessionDataAccess>();
        builder.Services.AddTransient<StartArkNovaSessionUseCase>();
        builder.Services.AddTransient<IArkNovaRunningSessionQuery, SqliteArkNovaRunningSessionQuery>();
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

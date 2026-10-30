using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Infrastructure.Persistence.SQLite.History;
using BoardgameTracker.App.Games.ArkNova;
using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Features.History;
using BoardgameTracker.App.Features.Settings.AppSettings;
using BoardgameTracker.App.Services;
using BoardgameTracker.Application.Groups.CreateGroup;
using BoardgameTracker.Application.Groups.ListGroups;
using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.App.Features.Groups.Colors;
using BoardgameTracker.Application.GameSessions.Running;
using BoardgameTracker.Application.GameSessions.Resume;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;
using BoardgameTracker.Infrastructure.Persistence.SQLite.GameSessions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BoardgameTracker.App.Features.Players.ListPlayers;
using BoardgameTracker.App.Features.Players.EditPlayer;
using BoardgameTracker.Application.Players.ListPlayers;
using BoardgameTracker.Application.Players.EditPlayer;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Players;

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
#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton<BoardgameTracker.App.Theming.ThemeManager>();
        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            handlers.AddHandler<Shell, BoardgameTracker.App.Platforms.Android.ThemeShellRenderer>();
            handlers.AddHandler<BoardgameTracker.App.Presentation.Controls.NumberWheel, BoardgameTracker.App.Platforms.Android.NumberWheelHandler>();
#elif IOS
            handlers.AddHandler<Shell, BoardgameTracker.App.Platforms.iOS.ThemeShellRenderer>();
            handlers.AddHandler<BoardgameTracker.App.Presentation.Controls.NumberWheel, BoardgameTracker.App.Platforms.iOS.NumberWheelHandler>();
#endif
        });

        string databasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "boardgame-tracker.db");

        builder.Services.AddDbContextFactory<BoardgameTrackerDbContext>(
            options => BoardgameTrackerDbContextOptions.Configure(
                options,
                $"Data Source={databasePath}"));

        builder.Services.AddArkNova();
        builder.Services.AddSingleton<BgtFormat>();
        builder.Services.AddTransient<HistoryTransferUseCase>();
        builder.Services.AddSingleton<IHistoryTransferStore, SqliteHistoryTransferStore>();
        builder.Services.AddSingleton<DatabaseInitializer>();

        builder.Services.AddSingleton<IListGroupsQuery, SqliteListGroupsQuery>();
        builder.Services.AddSingleton<IListPlayersQuery, SqliteListPlayersQuery>();
        builder.Services.AddTransient<IEditPlayerDataAccess, SqliteEditPlayerDataAccess>();
        builder.Services.AddTransient<EditPlayerUseCase>();
        builder.Services.AddTransient<PlayerListViewModel>();
        builder.Services.AddTransient<PlayerListPage>();
        builder.Services.AddTransient<EditPlayerViewModel>();
        builder.Services.AddTransient<EditPlayerPage>();
        builder.Services.AddTransient<ICreateGroupDataAccess, SqliteCreateGroupDataAccess>();
        builder.Services.AddTransient<CreateGroupUseCase>();
        builder.Services.AddSingleton<GamePlayerColorCatalog>();
        builder.Services.AddTransient<GroupColorSelectionViewModel>();
        builder.Services.AddTransient<IEditGroupDataAccess, SqliteEditGroupDataAccess>();
        builder.Services.AddTransient<EditGroupUseCase>();
        builder.Services.AddTransient<IRunningGameSessionDataAccess, SqliteRunningGameSessionDataAccess>();
        builder.Services.AddTransient<RunningGameSessionUseCase>();
        builder.Services.AddTransient<IOpenGameSessionQuery, SqliteOpenGameSessionQuery>();
        builder.Services.AddSingleton<IAppNavigationSettingsStore, MauiAppNavigationSettingsStore>();

        builder.Services.AddSingleton<GroupListViewModel>();
        builder.Services.AddSingleton<GroupListPage>();
        builder.Services.AddSingleton<GamesPage>();
        builder.Services.AddSingleton<HistoryPage>();
        builder.Services.AddSingleton<BoardgameTracker.Application.History.IHistoryQuery, SqliteHistoryQuery>();
        builder.Services.AddSingleton<AppSettingsPage>();
        builder.Services.AddTransient<CreateGroupViewModel>();
        builder.Services.AddTransient<CreateGroupPage>();
        builder.Services.AddTransient<EditGroupViewModel>();
        builder.Services.AddTransient<EditGroupPage>();

        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}

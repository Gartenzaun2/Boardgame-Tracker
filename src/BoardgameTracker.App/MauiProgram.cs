using BoardgameTracker.App.Persistence.SQLite;
using BoardgameTracker.App.Features.History.Export;
using BoardgameTracker.Application.History.Transfer;
using BoardgameTracker.Application.History;
using BoardgameTracker.App.Games.ArkNova;
using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Features.History;
using BoardgameTracker.App.Features.Settings.AppSettings;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.Groups.CreateGroup;
using BoardgameTracker.Application.Groups.ListGroups;
using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.App.Features.Groups.Colors;
using BoardgameTracker.Application.GameSessions.Running;
using BoardgameTracker.Application.GameSessions.Resume;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Application.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BoardgameTracker.App.Features.Players.ListPlayers;
using BoardgameTracker.App.Features.Players.EditPlayer;
using BoardgameTracker.Application.Players.ListPlayers;
using BoardgameTracker.Application.Players.EditPlayer;

namespace BoardgameTracker.App;

/// <summary>
/// Composition root for MAUI, EF Core, Application use cases, queries, pages and view models.
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

        builder.Services.AddBoardgameTrackerSqlite(databasePath);

        builder.Services.AddArkNova();
        builder.Services.AddSingleton<BgtFormat>();
        builder.Services.AddTransient<HistoryTransferUseCase>();
        builder.Services.AddSingleton<HistoryExportFileSaver>();
        builder.Services.AddSingleton<IHistoryExportTarget, SaveHistoryExportTarget>();
        builder.Services.AddSingleton<IHistoryExportTarget, ShareHistoryExportTarget>();
        builder.Services.AddTransient<HistoryExportFlow>();
        builder.Services.AddSingleton<HistoryTransferPersistence>();
        builder.Services.AddSingleton<DatabaseInitializer>();

        builder.Services.AddSingleton<ListGroupsQuery>();
        builder.Services.AddSingleton<ListPlayersQuery>();
        builder.Services.AddTransient<EditPlayerUseCase>();
        builder.Services.AddTransient<PlayerListViewModel>();
        builder.Services.AddTransient<PlayerListPage>();
        builder.Services.AddTransient<EditPlayerViewModel>();
        builder.Services.AddTransient<EditPlayerPage>();
        builder.Services.AddTransient<CreateGroupUseCase>();
        builder.Services.AddSingleton<GamePlayerColorCatalog>();
        builder.Services.AddTransient<GroupColorSelectionViewModel>();
        builder.Services.AddTransient<EditGroupUseCase>();
        builder.Services.AddTransient<RunningGameSessionUseCase>();
        builder.Services.AddTransient<OpenGameSessionQuery>();
        builder.Services.AddSingleton<IAppNavigationSettingsStore, MauiAppNavigationSettingsStore>();

        builder.Services.AddSingleton<GroupListViewModel>();
        builder.Services.AddSingleton<GroupListPage>();
        builder.Services.AddSingleton<GamesPage>();
        builder.Services.AddSingleton<HistoryPage>();
        builder.Services.AddSingleton<CompletedGameSessionsQuery>();
        builder.Services.AddSingleton<AppSettingsPage>();
        builder.Services.AddTransient<CreateGroupViewModel>();
        builder.Services.AddTransient<CreateGroupPage>();
        builder.Services.AddTransient<EditGroupViewModel>();
        builder.Services.AddTransient<EditGroupPage>();

        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}

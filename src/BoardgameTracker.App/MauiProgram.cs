using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.GameSessions.Preparation;
using BoardgameTracker.Application.Groups.CreateGroup;
using BoardgameTracker.Application.Groups.ListGroups;
using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Application.Games.ArkNova.Preparation;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Games.ArkNova;
using Microsoft.Extensions.DependencyInjection;

namespace BoardgameTracker.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { });

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

        builder.Services.AddSingleton<GroupListViewModel>();
        builder.Services.AddSingleton<GroupListPage>();
        builder.Services.AddTransient<CreateGroupViewModel>();
        builder.Services.AddTransient<CreateGroupPage>();
        builder.Services.AddTransient<EditGroupViewModel>();
        builder.Services.AddTransient<EditGroupPage>();
        builder.Services.AddTransient<ArkNovaPreparationViewModel>();
        builder.Services.AddTransient<ArkNovaPreparationPage>();

        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}

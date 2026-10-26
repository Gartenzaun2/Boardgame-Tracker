using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.Application.Groups.CreateGroup;
using BoardgameTracker.Application.Groups.ListGroups;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;
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

        builder.Services.AddSingleton<GroupListViewModel>();
        builder.Services.AddSingleton<GroupListPage>();
        builder.Services.AddTransient<CreateGroupViewModel>();
        builder.Services.AddTransient<CreateGroupPage>();

        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}

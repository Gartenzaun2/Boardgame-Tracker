using BoardgameTracker.App.Features.Groups;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using Microsoft.Extensions.DependencyInjection;

namespace BoardgameTracker.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
            });

        string databasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "boardgame-tracker.db");

        builder.Services.AddDbContextFactory<BoardgameTrackerDbContext>(
            options => BoardgameTrackerDbContextOptions.Configure(
                options,
                $"Data Source={databasePath}"));

        builder.Services.AddSingleton<DatabaseInitializer>();

        builder.Services.AddSingleton<GroupListViewModel>();
        builder.Services.AddSingleton<GroupListPage>();
        builder.Services.AddSingleton<AppShell>();
        return builder.Build();
    }
}

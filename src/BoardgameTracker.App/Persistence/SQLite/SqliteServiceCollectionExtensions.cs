using BoardgameTracker.Application.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace BoardgameTracker.App.Persistence.SQLite;

public static class SqliteServiceCollectionExtensions
{
    public static IServiceCollection AddBoardgameTrackerSqlite(this IServiceCollection services, string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        string connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ConnectionString;
        return services.AddDbContextFactory<BoardgameTrackerDbContext>(options =>
            Configure(options, connectionString));
    }

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlite(connectionString)
            .AddInterceptors(new SqliteNameCollationInterceptor())
            .ConfigureWarnings(warnings => warnings.Throw(CoreEventId.ShadowPropertyCreated));
    }
}

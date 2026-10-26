using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite;

public static class BoardgameTrackerDbContextOptions
{
    public static void Configure(
        DbContextOptionsBuilder optionsBuilder,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        optionsBuilder
            .UseSqlite(connectionString)
            .ConfigureWarnings(warnings =>
                warnings.Throw(CoreEventId.ShadowPropertyCreated));
    }
}

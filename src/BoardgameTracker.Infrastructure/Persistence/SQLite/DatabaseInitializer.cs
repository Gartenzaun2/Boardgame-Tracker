using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite;

public sealed class DatabaseInitializer(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}

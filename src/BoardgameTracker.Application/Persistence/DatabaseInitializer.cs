using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Persistence;

/// <summary>
/// Creates the local development database when needed and ensures mandatory
/// application reference data exists before database-backed screens are shown.
/// </summary>
public sealed class DatabaseInitializer(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory,
    IEnumerable<IDatabaseInitializationStep> initializationSteps)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        foreach (IDatabaseInitializationStep step in initializationSteps)
            await step.ExecuteAsync(dbContext, cancellationToken);
    }
}

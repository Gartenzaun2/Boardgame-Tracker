namespace BoardgameTracker.Infrastructure.Persistence.SQLite;

public interface IDatabaseInitializationStep
{
    Task ExecuteAsync(
        BoardgameTrackerDbContext dbContext,
        CancellationToken cancellationToken = default);
}

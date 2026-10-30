namespace BoardgameTracker.Application.Persistence;

public interface IDatabaseInitializationStep
{
    Task ExecuteAsync(
        BoardgameTrackerDbContext dbContext,
        CancellationToken cancellationToken = default);
}

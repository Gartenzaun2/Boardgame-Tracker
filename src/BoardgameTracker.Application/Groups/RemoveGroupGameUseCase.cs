using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Groups;

public sealed class RemoveGroupGameUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task RemoveGroupGameAsync(Guid groupId, Guid gameId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(gameId, Guid.Empty);
        await using BoardgameTrackerDbContext dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        // Check current sessions inside the transaction that removes the assignment.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (await dbContext.GameSessions.AnyAsync(session => session.GroupId == groupId && session.GameId == gameId
            && (session.Status == GameSessionStatus.Running || session.Status == GameSessionStatus.Scoring), cancellationToken))
            throw new InvalidOperationException("A group game assignment with an open game session cannot be removed.");

        await dbContext.GroupGames.Where(assignment => assignment.GroupId == groupId && assignment.GameId == gameId)
            .ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

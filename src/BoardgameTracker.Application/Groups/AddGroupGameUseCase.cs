using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Groups;

public sealed class AddGroupGameUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task AddGroupGameAsync(Guid groupId, Guid gameId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(gameId, Guid.Empty);
        await using BoardgameTrackerDbContext dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        // Existence checks and idempotent insertion share the write transaction.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (!await dbContext.Groups.AnyAsync(group => group.Id == groupId, cancellationToken))
            throw new InvalidOperationException("The group does not exist.");
        if (!await dbContext.Games.AnyAsync(game => game.Id == gameId, cancellationToken))
            throw new InvalidOperationException("The game does not exist.");
        if (!await dbContext.GroupGames.AnyAsync(assignment => assignment.GroupId == groupId && assignment.GameId == gameId, cancellationToken))
        {
            dbContext.GroupGames.Add(new GroupGame(groupId, gameId));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }
}

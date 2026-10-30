using BoardgameTracker.Core.Players;
using BoardgameTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Players;

public sealed class EditPlayerUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task<Player?> GetAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        await using BoardgameTrackerDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Players.AsNoTracking().SingleOrDefaultAsync(player => player.Id == playerId, cancellationToken);
    }

    public async Task UpdatePlayerAsync(Guid playerId, string name, bool isArchived, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        await using BoardgameTrackerDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        Player player = await db.Players.SingleOrDefaultAsync(player => player.Id == playerId, cancellationToken)
            ?? throw new InvalidOperationException("The player does not exist.");
        player.SetName(name);
        player.SetArchived(isArchived);
        await db.SaveChangesAsync(cancellationToken);
    }
}

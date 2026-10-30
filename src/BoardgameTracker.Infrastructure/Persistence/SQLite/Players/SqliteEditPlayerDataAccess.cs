using BoardgameTracker.Application.Players.EditPlayer;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Players;

public sealed class SqliteEditPlayerDataAccess(IDbContextFactory<BoardgameTrackerDbContext> factory) : IEditPlayerDataAccess
{
    public async Task<Player?> GetAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Players.AsNoTracking().SingleOrDefaultAsync(player => player.Id == playerId, cancellationToken);
    }

    public async Task SaveAsync(Player player, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        db.Attach(player);
        db.Entry(player).Property(value => value.Name).IsModified = true;
        db.Entry(player).Property(value => value.IsArchived).IsModified = true;
        await db.SaveChangesAsync(cancellationToken);
    }
}

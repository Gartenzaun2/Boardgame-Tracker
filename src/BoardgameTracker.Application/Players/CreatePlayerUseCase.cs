using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Players;

public sealed class CreatePlayerUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task<Guid> CreatePlayerAsync(string name, bool isGuest, CancellationToken cancellationToken = default)
    {
        Player player = new(name, isGuest);
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        db.Players.Add(player);
        await db.SaveChangesAsync(cancellationToken);
        return player.Id;
    }
}

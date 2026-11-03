using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Players;

public sealed class CreatePlayerUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task<CreatePlayerResult> CreatePlayerAsync(string name, bool isGuest, CancellationToken cancellationToken = default)
    {
        Player player = new(name, isGuest);
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        bool isFirst = !await db.Players.AnyAsync(cancellationToken);
        db.Players.Add(player);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(player.Id, isFirst);
    }
}

public sealed record CreatePlayerResult(Guid Id, bool IsFirstPlayer);

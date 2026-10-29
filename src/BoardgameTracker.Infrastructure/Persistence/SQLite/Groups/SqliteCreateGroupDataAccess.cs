using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Application.Groups.CreateGroup;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;

/// <summary>
/// SQLite implementation of the create-group slice persistence boundary.
/// </summary>
public sealed class SqliteCreateGroupDataAccess(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory,
    IEnumerable<IGroupColorPersistence> colorStores)
    : ICreateGroupDataAccess
{
    public async Task<IReadOnlyList<SelectablePlayer>> GetSelectablePlayersAsync(
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Players
            .AsNoTracking()
            .Where(player => player.IsActive)
            .OrderBy(player => player.Name)
            .Select(player => new SelectablePlayer(
                player.Id,
                player.Name,
                player.IsGuest))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetActivePlayerIdsAsync(
        IReadOnlyCollection<Guid> playerIds,
        CancellationToken cancellationToken = default)
    {
        if (playerIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        Guid[] requestedIds = [.. playerIds];

        Guid[] ids = await dbContext.Players
            .AsNoTracking()
            .Where(player => player.IsActive && requestedIds.Contains(player.Id))
            .Select(player => player.Id)
            .ToArrayAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task SaveAsync(
        Group group,
        IReadOnlyCollection<Player> newPlayers,
        GroupColorDefaults? colors,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(newPlayers);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (newPlayers.Count > 0)
        {
            dbContext.Players.AddRange(newPlayers);
        }

        // Group owns GroupPlayer. EF tracks the backing collection and inserts
        // the memberships together with the aggregate.
        dbContext.Groups.Add(group);

        if (colors is not null)
        {
            var store = colorStores.SingleOrDefault(store => store.GameKey == colors.GameKey);
            if (store is not null) await store.ReplaceAsync(dbContext, group.Id, colors.Colors, cancellationToken);
            else if (colors.Colors.Values.Any(color => color.HasValue))
                throw new InvalidOperationException("No color storage registered for this game.");
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

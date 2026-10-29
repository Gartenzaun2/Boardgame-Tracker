using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;

/// <summary>
/// SQLite implementation of the group-edit slice persistence boundary.
/// </summary>
public sealed class SqliteEditGroupDataAccess(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory,
    IEnumerable<IGroupColorPersistence> colorStores)
    : IEditGroupDataAccess
{
    public async Task<EditGroupDetails?> GetDetailsAsync(
        Guid groupId,
        string? gameKey,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var group = await dbContext.Groups
            .AsNoTracking()
            .Where(candidate => candidate.Id == groupId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Name,
                candidate.IsTemporary,
                candidate.IsArchived
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (group is null)
        {
            return null;
        }

        EditGroupPlayer[] players = await (
            from membership in dbContext.GroupPlayers.AsNoTracking()
            join player in dbContext.Players.AsNoTracking()
                on membership.PlayerId equals player.Id
            where membership.GroupId == groupId
            orderby membership.DefaultOrder
            select new EditGroupPlayer(
                player.Id,
                player.Name,
                player.IsGuest,
                membership.DefaultOrder))
            .ToArrayAsync(cancellationToken);

        var store = colorStores.SingleOrDefault(store => store.GameKey == gameKey);
        IReadOnlyDictionary<Guid, PlayerColor?> colors = store is null
            ? new Dictionary<Guid, PlayerColor?>()
            : await store.ReadAsync(dbContext, groupId, cancellationToken);
        players = [.. players.Select(player => player with { DefaultColor = colors.GetValueOrDefault(player.PlayerId) })];

        return new EditGroupDetails(
            group.Id,
            group.Name,
            group.IsTemporary,
            group.IsArchived,
            players);
    }

    public async Task<Group?> GetAggregateAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Groups
            .Include(group => group.Players)
            .SingleOrDefaultAsync(group => group.Id == groupId, cancellationToken);
    }

    public async Task SaveAsync(
        Group group,
        GroupColorDefaults? colors,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await SaveGroupChangesAsync(dbContext, group, cancellationToken);
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

    private static async Task SaveGroupChangesAsync(
        BoardgameTrackerDbContext dbContext, Group group, CancellationToken cancellationToken)
    {
        int updatedGroups = await dbContext.Groups
            .Where(candidate => candidate.Id == group.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(candidate => candidate.Name, group.Name)
                    .SetProperty(candidate => candidate.IsTemporary, group.IsTemporary)
                    .SetProperty(candidate => candidate.IsArchived, group.IsArchived),
                cancellationToken);

        if (updatedGroups != 1)
        {
            throw new InvalidOperationException("The group no longer exists.");
        }

        // Move all persisted orders outside the normal 1..n range first.
        // This avoids transient collisions with the unique (GroupId, DefaultOrder)
        // index while two players swap their positions.
        int orderOffset = group.Players.Count + 1000;

        await dbContext.GroupPlayers
            .Where(player => player.GroupId == group.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    player => player.DefaultOrder,
                    player => player.DefaultOrder + orderOffset),
                cancellationToken);

        foreach (GroupPlayer player in group.Players.OrderBy(player => player.DefaultOrder))
        {
            int updatedPlayers = await dbContext.GroupPlayers
                .Where(candidate =>
                    candidate.GroupId == group.Id
                    && candidate.PlayerId == player.PlayerId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        candidate => candidate.DefaultOrder,
                        player.DefaultOrder),
                    cancellationToken);

            if (updatedPlayers != 1)
            {
                throw new InvalidOperationException(
                    "A group player no longer exists.");
            }
        }

    }
}

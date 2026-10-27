using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Core.Games.ArkNova.Groups;
using BoardgameTracker.Core.Games.ArkNova.Players;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;

/// <summary>
/// SQLite implementation of the group-edit slice persistence boundary.
/// </summary>
public sealed class SqliteEditGroupDataAccess(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IEditGroupDataAccess
{
    public async Task<EditGroupDetails?> GetDetailsAsync(
        Guid groupId,
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
            join arkNovaPlayer in dbContext.ArkNovaGroupPlayers.AsNoTracking()
                on new { membership.GroupId, membership.PlayerId }
                equals new { arkNovaPlayer.GroupId, arkNovaPlayer.PlayerId }
                into arkNovaPlayers
            from arkNovaPlayer in arkNovaPlayers.DefaultIfEmpty()
            where membership.GroupId == groupId
            orderby membership.DefaultOrder
            select new EditGroupPlayer(
                player.Id,
                player.Name,
                player.IsGuest,
                membership.DefaultOrder,
                arkNovaPlayer == null
                    ? (ArkNovaPlayerColor?)null
                    : arkNovaPlayer.DefaultColor))
            .ToArrayAsync(cancellationToken);

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
        IReadOnlyDictionary<Guid, ArkNovaPlayerColor?> arkNovaDefaultColors,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(arkNovaDefaultColors);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

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

        // Default colors form a tiny replaceable group-level snapshot. Replacing
        // the validated snapshot avoids transient unique-index collisions when two
        // players swap colors in one edit operation. The surrounding transaction
        // keeps the delete-and-reinsert sequence atomic.
        await dbContext.ArkNovaGroupPlayers
            .Where(player => player.GroupId == group.Id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach ((Guid playerId, ArkNovaPlayerColor? color) in arkNovaDefaultColors)
        {
            if (color is ArkNovaPlayerColor selectedColor)
            {
                dbContext.ArkNovaGroupPlayers.Add(
                    new ArkNovaGroupPlayer(group.Id, playerId, selectedColor));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}

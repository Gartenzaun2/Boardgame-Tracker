using BoardgameTracker.Application.Groups.EditGroup;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;

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

        EditGroupMember[] members = await (
            from membership in dbContext.GroupPersons.AsNoTracking()
            join person in dbContext.Persons.AsNoTracking()
                on membership.PersonId equals person.Id
            join arkNovaPlayer in dbContext.ArkNovaGroupPlayers.AsNoTracking()
                on new { membership.GroupId, membership.PersonId }
                equals new { arkNovaPlayer.GroupId, arkNovaPlayer.PersonId }
                into arkNovaPlayers
            from arkNovaPlayer in arkNovaPlayers.DefaultIfEmpty()
            where membership.GroupId == groupId
            orderby membership.DefaultOrder
            select new EditGroupMember(
                person.Id,
                person.Name,
                person.IsGuest,
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
            members);
    }

    public async Task<Group?> GetAggregateAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Groups
            .Include(group => group.Members)
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
        // index while two members swap their positions.
        int orderOffset = group.Members.Count + 1000;

        await dbContext.GroupPersons
            .Where(member => member.GroupId == group.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    member => member.DefaultOrder,
                    member => member.DefaultOrder + orderOffset),
                cancellationToken);

        foreach (GroupPerson member in group.Members.OrderBy(member => member.DefaultOrder))
        {
            int updatedMembers = await dbContext.GroupPersons
                .Where(candidate =>
                    candidate.GroupId == group.Id
                    && candidate.PersonId == member.PersonId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        candidate => candidate.DefaultOrder,
                        member.DefaultOrder),
                    cancellationToken);

            if (updatedMembers != 1)
            {
                throw new InvalidOperationException(
                    "A group member no longer exists.");
            }
        }

        ArkNovaGroupPlayer[] existingDefaults = await dbContext.ArkNovaGroupPlayers
            .Where(player => player.GroupId == group.Id)
            .ToArrayAsync(cancellationToken);

        foreach ((Guid personId, ArkNovaPlayerColor? color) in arkNovaDefaultColors)
        {
            ArkNovaGroupPlayer? existing = existingDefaults
                .SingleOrDefault(player => player.PersonId == personId);

            if (color is null)
            {
                if (existing is not null)
                {
                    dbContext.ArkNovaGroupPlayers.Remove(existing);
                }

                continue;
            }

            if (existing is null)
            {
                dbContext.ArkNovaGroupPlayers.Add(
                    new ArkNovaGroupPlayer(group.Id, personId, color.Value));
            }
            else
            {
                existing.SetDefaultColor(color.Value);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}

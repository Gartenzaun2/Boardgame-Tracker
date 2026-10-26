using BoardgameTracker.Application.Groups.CreateGroup;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.People;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;

public sealed class SqliteCreateGroupDataAccess(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : ICreateGroupDataAccess
{
    public async Task<IReadOnlyList<SelectablePerson>> GetSelectablePeopleAsync(
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Persons
            .AsNoTracking()
            .Where(person => person.IsActive)
            .OrderBy(person => person.Name)
            .Select(person => new SelectablePerson(
                person.Id,
                person.Name,
                person.IsGuest))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetActivePersonIdsAsync(
        IReadOnlyCollection<Guid> personIds,
        CancellationToken cancellationToken = default)
    {
        if (personIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        Guid[] requestedIds = personIds.ToArray();

        Guid[] ids = await dbContext.Persons
            .AsNoTracking()
            .Where(person => person.IsActive && requestedIds.Contains(person.Id))
            .Select(person => person.Id)
            .ToArrayAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task SaveAsync(
        Group group,
        IReadOnlyCollection<Person> newPeople,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(newPeople);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        if (newPeople.Count > 0)
        {
            dbContext.Persons.AddRange(newPeople);
        }

        // Group owns GroupPerson. EF tracks the backing collection and inserts
        // the memberships together with the aggregate.
        dbContext.Groups.Add(group);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

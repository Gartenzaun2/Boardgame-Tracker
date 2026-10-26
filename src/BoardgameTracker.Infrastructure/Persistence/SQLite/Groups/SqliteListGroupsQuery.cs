using BoardgameTracker.Application.Groups.ListGroups;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;

public sealed class SqliteListGroupsQuery(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IListGroupsQuery
{
    public async Task<IReadOnlyList<GroupListItem>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var groups = await dbContext.Groups
            .AsNoTracking()
            .Where(group => !group.IsTemporary)
            .Select(group => new { group.Id, group.Name })
            .ToListAsync(cancellationToken);

        var members = await (
            from membership in dbContext.GroupPersons.AsNoTracking()
            join person in dbContext.Persons.AsNoTracking()
                on membership.PersonId equals person.Id
            select new
            {
                membership.GroupId,
                membership.DefaultOrder,
                person.Name
            })
            .ToListAsync(cancellationToken);

        // SQLite stores DateTimeOffset, but provider-side ordering/aggregation is limited.
        // We therefore fetch only the two required columns and calculate Max in memory.
        var sessionDates = await dbContext.GameSessions
            .AsNoTracking()
            .Select(session => new { session.GroupId, session.StartedAt })
            .ToListAsync(cancellationToken);

        Dictionary<Guid, DateTimeOffset?> lastPlayedByGroup = sessionDates
            .GroupBy(session => session.GroupId)
            .ToDictionary(
                group => group.Key,
                group => (DateTimeOffset?)group.Max(session => session.StartedAt));

        Dictionary<Guid, string[]> memberNamesByGroup = members
            .GroupBy(member => member.GroupId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(member => member.DefaultOrder)
                    .Select(member => member.Name)
                    .ToArray());

        return groups
            .Select(group =>
            {
                string[] memberNames = memberNamesByGroup.GetValueOrDefault(group.Id) ?? [];

                return new GroupListItem(
                    group.Id,
                    group.Name,
                    memberNames.Length,
                    memberNames,
                    lastPlayedByGroup.GetValueOrDefault(group.Id));
            })
            .OrderByDescending(group => group.LastPlayedAt)
            .ThenBy(group => group.Name)
            .ToArray();
    }
}

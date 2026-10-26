using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.People;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;

public sealed class GroupStore(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task<IReadOnlyList<GroupSummary>> GetGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        List<Group> groups = await dbContext.Groups
            .AsNoTracking()
            .Where(group => !group.IsTemporary)
            .Include(group => group.Members)
            .ThenInclude(member => member.Person)
            .ToListAsync(cancellationToken);

        var sessionDates = await dbContext.GameSessions
            .AsNoTracking()
            .Select(session => new { session.GroupId, session.StartedAt })
            .ToListAsync(cancellationToken);

        Dictionary<Guid, DateTimeOffset?> lastPlayedByGroup = sessionDates
            .GroupBy(session => session.GroupId)
            .ToDictionary(
                group => group.Key,
                group => (DateTimeOffset?)group.Max(session => session.StartedAt));

        return groups
            .Select(group => new GroupSummary(
                group.Id,
                group.Name,
                group.IsTemporary,
                group.Members.Count,
                group.Members
                    .OrderBy(member => member.DefaultOrder)
                    .Select(member => member.Person?.Name ?? string.Empty)
                    .Where(name => name.Length > 0)
                    .ToArray(),
                lastPlayedByGroup.GetValueOrDefault(group.Id)))
            .OrderByDescending(group => group.LastPlayedAt)
            .ThenBy(group => group.Name)
            .ToArray();
    }

    public async Task<IReadOnlyList<PersonSummary>> GetActivePersonsAsync(
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Persons
            .AsNoTracking()
            .Where(person => person.IsActive)
            .OrderBy(person => person.Name)
            .Select(person => new PersonSummary(
                person.Id,
                person.Name,
                person.IsGuest))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateGroupAsync(
        CreateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        if (request.Members.Count == 0)
        {
            throw new ArgumentException(
                "A group must contain at least one person.",
                nameof(request));
        }

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        Group group = new(request.Name, request.IsTemporary);
        dbContext.Groups.Add(group);

        HashSet<Guid> memberIds = [];

        for (int index = 0; index < request.Members.Count; index++)
        {
            CreateGroupMember member = request.Members[index];
            Person person;

            if (member.ExistingPersonId is Guid existingPersonId)
            {
                person = await dbContext.Persons.SingleAsync(
                    candidate => candidate.Id == existingPersonId && candidate.IsActive,
                    cancellationToken);
            }
            else
            {
                person = new Person(member.Name, member.IsGuest);
                dbContext.Persons.Add(person);
            }

            if (!memberIds.Add(person.Id))
            {
                throw new InvalidOperationException(
                    "A person can only be added to a group once.");
            }

            dbContext.GroupPersons.Add(
                new GroupPerson(group.Id, person.Id, index + 1));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return group.Id;
    }
}

public sealed record GroupSummary(
    Guid Id,
    string Name,
    bool IsTemporary,
    int MemberCount,
    IReadOnlyList<string> MemberNames,
    DateTimeOffset? LastPlayedAt);

public sealed record PersonSummary(
    Guid Id,
    string Name,
    bool IsGuest);

public sealed record CreateGroupRequest(
    string Name,
    bool IsTemporary,
    IReadOnlyList<CreateGroupMember> Members);

public sealed record CreateGroupMember(
    Guid? ExistingPersonId,
    string Name,
    bool IsGuest);

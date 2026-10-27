using BoardgameTracker.Application.Groups.ListGroups;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;

/// <summary>
/// SQLite implementation of the game-context group-overview read projection.
/// Groups remain game-independent; session metadata is filtered to the selected game.
/// </summary>
public sealed class SqliteListGroupsQuery(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IListGroupsQuery
{
    public async Task<IReadOnlyList<GroupListItem>> ExecuteAsync(
        string gameKey,
        bool includeTemporary = false,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var groups = await dbContext.Groups
            .AsNoTracking()
            .Where(group => includeTemporary || !group.IsTemporary)
            .Where(group => includeArchived || !group.IsArchived)
            .Select(group => new { group.Id, group.Name, group.IsTemporary, group.IsArchived })
            .ToListAsync(cancellationToken);

        Guid[] groupIds = [.. groups.Select(group => group.Id)];
        if (groupIds.Length == 0)
        {
            return [];
        }

        Guid? gameId = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.Key == gameKey)
            .Select(game => (Guid?)game.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var members = await (
            from membership in dbContext.GroupPersons.AsNoTracking()
            where groupIds.Contains(membership.GroupId)
            join person in dbContext.Persons.AsNoTracking()
                on membership.PersonId equals person.Id
            select new
            {
                membership.GroupId,
                membership.DefaultOrder,
                person.Name
            })
            .ToListAsync(cancellationToken);

        List<GroupSessionDateProjection> sessionDates = gameId is null
            ? []
            : await dbContext.GameSessions
                .AsNoTracking()
                .Where(session => groupIds.Contains(session.GroupId)
                                  && session.GameId == gameId.Value)
                .Select(session => new GroupSessionDateProjection(session.GroupId, session.StartedAt))
                .ToListAsync(cancellationToken);

        Dictionary<Guid, DateTimeOffset?> lastPlayedByGroup = sessionDates
            .GroupBy(session => session.GroupId)
            .ToDictionary(
                group => group.Key,
                group => (DateTimeOffset?)group.Max(session => session.StartedAt));

        List<GroupOpenSessionProjection> openSessions = gameId is null
            ? []
            : await dbContext.GameSessions
                .AsNoTracking()
                .Where(session => groupIds.Contains(session.GroupId)
                                  && session.GameId == gameId.Value)
                .Where(session => session.Status == GameSessionStatus.Running
                               || session.Status == GameSessionStatus.Scoring)
                .Select(session => new GroupOpenSessionProjection(
                    session.GroupId,
                    session.Id,
                    session.Status,
                    session.ModifiedAt))
                .ToListAsync(cancellationToken);

        var openSessionByGroup = openSessions
            .GroupBy(session => session.GroupId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(session => session.ModifiedAt).First());

        Dictionary<Guid, string[]> memberNamesByGroup = members
            .GroupBy(member => member.GroupId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(member => member.DefaultOrder)
                    .Select(member => member.Name)
                    .ToArray());

        return
        [
            .. groups
                .Select(group =>
                {
                    string[] memberNames = memberNamesByGroup.GetValueOrDefault(group.Id) ?? [];

                    return new GroupListItem(
                        group.Id,
                        group.Name,
                        group.IsTemporary,
                        group.IsArchived,
                        memberNames.Length,
                        memberNames,
                        lastPlayedByGroup.GetValueOrDefault(group.Id),
                        openSessionByGroup.TryGetValue(group.Id, out var openSession)
                            ? openSession.Id
                            : null,
                        openSessionByGroup.TryGetValue(group.Id, out openSession)
                            ? openSession.Status
                            : null);
                })
                .OrderByDescending(group => group.LastPlayedAt)
                .ThenBy(group => group.Name)
        ];
    }


    private sealed record GroupSessionDateProjection(Guid GroupId, DateTimeOffset StartedAt);

    private sealed record GroupOpenSessionProjection(
        Guid GroupId,
        Guid Id,
        GameSessionStatus Status,
        DateTimeOffset ModifiedAt);
}

using BoardgameTracker.Application.Games.ArkNova.Preparation;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Games.ArkNova;

public sealed class SqliteArkNovaPreparationQuery(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IArkNovaPreparationQuery
{
    public async Task<ArkNovaPreparationData?> ExecuteAsync(
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
                candidate.IsArchived
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (group is null)
        {
            return null;
        }

        var players = await (
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
            select new ArkNovaPreparationPlayer(
                person.Id,
                person.Name,
                membership.DefaultOrder,
                arkNovaPlayer == null
                    ? (ArkNovaPlayerColor?)null
                    : arkNovaPlayer.DefaultColor))
            .ToArrayAsync(cancellationToken);


        // SQLite cannot translate ORDER BY for DateTimeOffset. The number of
        // sessions for one group is small, so load the minimal projection and
        // choose the latest session on the client.
        var groupSessions = await dbContext.GameSessions
            .AsNoTracking()
            .Where(session => session.GroupId == groupId)
            .Select(session => new
            {
                session.Id,
                session.StartedAt
            })
            .ToArrayAsync(cancellationToken);

        var lastSession = groupSessions
            .OrderByDescending(session => session.StartedAt)
            .FirstOrDefault();

        ArkNovaLastSession? lastSessionData = null;

        if (lastSession is not null)
        {
            var placements = await (
                from sessionPlayer in dbContext.GameSessionPlayers.AsNoTracking()
                join person in dbContext.Persons.AsNoTracking()
                    on sessionPlayer.PersonId equals person.Id
                where sessionPlayer.GameSessionId == lastSession.Id
                      && sessionPlayer.Placement != null
                select new
                {
                    PersonId = person.Id,
                    person.Name,
                    Placement = sessionPlayer.Placement!.Value
                })
                .ToArrayAsync(cancellationToken);

            var winner = placements
                .Where(player => player.Placement == 1)
                .OrderBy(player => player.Name)
                .FirstOrDefault();

            var lastPlace = placements
                .OrderByDescending(player => player.Placement)
                .ThenBy(player => player.Name)
                .FirstOrDefault();

            lastSessionData = new ArkNovaLastSession(
                lastSession.StartedAt,
                lastPlace?.PersonId,
                winner?.Name);
        }

        return new ArkNovaPreparationData(
            group.Id,
            group.Name,
            group.IsArchived,
            lastSessionData,
            players);
    }
}

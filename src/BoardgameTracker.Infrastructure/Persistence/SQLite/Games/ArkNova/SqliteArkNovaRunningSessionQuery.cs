using BoardgameTracker.Application.Games.ArkNova.Running;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Games.ArkNova;

/// <summary>
/// SQLite projection for the running Ark Nova screen, mapped to Application read models after provider-side materialization.
/// </summary>
public sealed class SqliteArkNovaRunningSessionQuery(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IArkNovaRunningSessionQuery
{
    public async Task<ArkNovaRunningSessionData?> ExecuteAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var session = await (
            from gameSession in dbContext.GameSessions.AsNoTracking()
            join boardgameGroup in dbContext.Groups.AsNoTracking()
                on gameSession.GroupId equals boardgameGroup.Id
            where gameSession.Id == sessionId
                  && gameSession.Status == GameSessionStatus.Running
            select new
            {
                SessionId = gameSession.Id,
                GroupName = boardgameGroup.Name,
                gameSession.StartedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return null;
        }

        // Materialize a simple provider-friendly projection first. The Application
        // read-model records are created afterwards so EF Core only has to translate
        // columns/joins and not an application-specific constructor projection.
        var playerRows = await (
            from sessionPlayer in dbContext.GameSessionPlayers.AsNoTracking()
            join person in dbContext.Persons.AsNoTracking()
                on sessionPlayer.PersonId equals person.Id
            join arkNovaPlayer in dbContext.ArkNovaSessionPlayers.AsNoTracking()
                on new { sessionPlayer.GameSessionId, sessionPlayer.PersonId }
                equals new { arkNovaPlayer.GameSessionId, arkNovaPlayer.PersonId }
            join zooPlan in dbContext.ArkNovaZooPlans.AsNoTracking()
                on arkNovaPlayer.SelectedZooPlanId equals zooPlan.Id
            where sessionPlayer.GameSessionId == sessionId
            orderby sessionPlayer.Order
            select new
            {
                PersonId = person.Id,
                person.Name,
                sessionPlayer.Order,
                arkNovaPlayer.Color,
                ZooPlanNumber = zooPlan.Number,
                ZooPlanNameResourceKey = zooPlan.NameResourceKey,
                ZooPlanIconSource = zooPlan.IconKey
            })
            .ToArrayAsync(cancellationToken);

        IReadOnlyList<ArkNovaRunningSessionPlayer> players =
        [
            .. playerRows.Select(player => new ArkNovaRunningSessionPlayer(
                player.PersonId,
                player.Name,
                player.Order,
                player.Color,
                player.ZooPlanNumber,
                player.ZooPlanNameResourceKey,
                player.ZooPlanIconSource))
        ];

        return new ArkNovaRunningSessionData(
            session.SessionId,
            session.GroupName,
            session.StartedAt,
            players);
    }
}

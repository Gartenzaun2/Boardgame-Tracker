using BoardgameTracker.Application.Games.ArkNova.GameSessions.Running;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Infrastructure.Persistence.SQLite;

namespace BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite;

/// <summary>
/// SQLite projection for the running Ark Nova screen, mapped to Application read models after provider-side materialization.
/// </summary>
public sealed class SqliteArkNovaRunningGameSessionQuery(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IArkNovaRunningGameSessionQuery
{
    public async Task<ArkNovaRunningGameSessionData?> ExecuteAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var gameSessionRow = await (
            from session in dbContext.GameSessions.AsNoTracking()
            join boardgameGroup in dbContext.Groups.AsNoTracking()
                on session.GroupId equals boardgameGroup.Id
            where session.Id == gameSessionId
                  && session.Status == GameSessionStatus.Running
            select new
            {
                GameSessionId = session.Id,
                GroupName = boardgameGroup.Name,
                session.StartedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (gameSessionRow is null)
        {
            return null;
        }

        // Materialize a simple provider-friendly projection first. The Application
        // read-model records are created afterwards so EF Core only has to translate
        // columns/joins and not an application-specific constructor projection.
        var playerRows = await (
            from gameSessionPlayer in dbContext.GameSessionPlayers.AsNoTracking()
            join player in dbContext.Players.AsNoTracking()
                on gameSessionPlayer.PlayerId equals player.Id
            join arkNovaPlayer in dbContext.ArkNovaGameSessionPlayers.AsNoTracking()
                on new { gameSessionPlayer.GameSessionId, gameSessionPlayer.PlayerId }
                equals new { arkNovaPlayer.GameSessionId, arkNovaPlayer.PlayerId }
            where gameSessionPlayer.GameSessionId == gameSessionId
            orderby gameSessionPlayer.Order
            select new
            {
                PlayerId = player.Id,
                player.Name,
                gameSessionPlayer.Order,
                arkNovaPlayer.Color,
                ZooPlanNumber = arkNovaPlayer.SelectedZooPlanNumber
            })
            .ToArrayAsync(cancellationToken);

        IReadOnlyList<ArkNovaRunningGameSessionPlayer> players =
        [
            .. playerRows.Select(player => new ArkNovaRunningGameSessionPlayer(
                player.PlayerId,
                player.Name,
                player.Order,
                player.Color,
                player.ZooPlanNumber))
        ];

        return new ArkNovaRunningGameSessionData(
            gameSessionRow.GameSessionId,
            gameSessionRow.GroupName,
            gameSessionRow.StartedAt,
            players);
    }
}

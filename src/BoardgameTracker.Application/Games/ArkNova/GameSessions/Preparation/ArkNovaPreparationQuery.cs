using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.GameSessions.Setup;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Core.Games.ArkNova.Groups;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Preparation;

/// <summary>
/// EF Core projection for Ark Nova preparation data, including group defaults and the last completed gameSession.
/// </summary>
public sealed class ArkNovaPreparationQuery(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task<ArkNovaPreparationData?> GetPreparationAsync(
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
            select new ArkNovaPreparationPlayer(
                player.Id,
                player.Name,
                membership.DefaultOrder,
                arkNovaPlayer == null
                    ? (PlayerColor?)null
                    : arkNovaPlayer.DefaultColor))
            .ToArrayAsync(cancellationToken);

        Guid? arkNovaGameId = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.Key == ArkNovaGame.Key)
            .Select(game => (Guid?)game.Id)
            .SingleOrDefaultAsync(cancellationToken);

        ArkNovaLastGameSession? lastGameSessionData = null;

        if (arkNovaGameId is Guid gameId)
        {
            // SQLite cannot translate ORDER BY for DateTimeOffset. The number of
            // completed gameSessions for one group is small, so load the minimal
            // projection and choose the latest gameSession on the client.
            var groupGameSessions = await dbContext.GameSessions
                .AsNoTracking()
                .Where(gameSession => gameSession.GroupId == groupId
                                  && gameSession.GameId == gameId
                                  && gameSession.Status == GameSessionStatus.Completed)
                .Select(gameSession => new
                {
                    gameSession.Id,
                    gameSession.StartedAt
                })
                .ToArrayAsync(cancellationToken);

            var lastGameSession = groupGameSessions
                .OrderByDescending(gameSession => gameSession.StartedAt)
                .FirstOrDefault();

            if (lastGameSession is not null)
            {
                ArkNovaPreviousPlacement[] placements = await (
                    from gameSessionPlayer in dbContext.GameSessionPlayers.AsNoTracking()
                    join player in dbContext.Players.AsNoTracking()
                        on gameSessionPlayer.PlayerId equals player.Id
                    where gameSessionPlayer.GameSessionId == lastGameSession.Id
                          && gameSessionPlayer.Placement != null
                    orderby gameSessionPlayer.Placement, player.Name
                    select new ArkNovaPreviousPlacement(
                        player.Id,
                        player.Name,
                        gameSessionPlayer.Placement!.Value))
                    .ToArrayAsync(cancellationToken);

                lastGameSessionData = new ArkNovaLastGameSession(
                    lastGameSession.StartedAt,
                    placements);
            }
        }

        return new ArkNovaPreparationData(
            group.Id,
            group.Name,
            group.IsArchived,
            lastGameSessionData,
            players);
    }
}

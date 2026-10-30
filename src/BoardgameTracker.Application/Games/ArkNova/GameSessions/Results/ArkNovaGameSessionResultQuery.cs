using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Application.Persistence;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions.Results;

public sealed class ArkNovaGameSessionResultQuery(IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task<ArkNovaGameSessionResultData?> LoadGameSessionResultAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var gameSession = await dbContext.GameSessions
            .AsNoTracking()
            .Where(x => x.Id == gameSessionId && dbContext.Games.Any(g => g.Id == x.GameId && g.Key == ArkNovaGame.Key))
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.StartedAt,
                x.EndedAt,
                GroupName = dbContext.Groups.Where(g => g.Id == x.GroupId).Select(g => g.Name).Single()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (gameSession is null)
        {
            return null;
        }

        var players = await (
            from gamePlayer in dbContext.GameSessionPlayers.AsNoTracking()
            join arkPlayer in dbContext.ArkNovaGameSessionPlayers.AsNoTracking()
                on new { gamePlayer.GameSessionId, gamePlayer.PlayerId }
                equals new { arkPlayer.GameSessionId, arkPlayer.PlayerId }
            join player in dbContext.Players.AsNoTracking() on gamePlayer.PlayerId equals player.Id
            where gamePlayer.GameSessionId == gameSessionId
            orderby gamePlayer.Order
            select new ArkNovaGameSessionResultPlayerData(
                gamePlayer.PlayerId,
                player.Name,
                gamePlayer.Order,
                arkPlayer.Color,
                arkPlayer.SelectedZooPlanNumber,
                arkPlayer.Attractiveness,
                arkPlayer.Conservation,
                arkPlayer.Reputation,
                gamePlayer.Placement))
            .ToListAsync(cancellationToken);

        List<ArkNovaSpecialCardAssignmentData> specialCards = await dbContext.ArkNovaGameSessionSpecialCardAssignments
            .AsNoTracking()
            .Where(x => x.GameSessionId == gameSessionId)
            .Select(x => new ArkNovaSpecialCardAssignmentData(x.PlayerId, x.SpecialCardId))
            .ToListAsync(cancellationToken);

        return new ArkNovaGameSessionResultData(
            gameSession.Id,
            gameSession.GroupName,
            gameSession.Status,
            gameSession.StartedAt,
            gameSession.EndedAt,
            players,
            specialCards)
        {
            CardCatalog = await dbContext.ArkNovaSpecialCards.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new ArkNovaSpecialCardData(c.Id, c.Name, c.Symbol1, c.Symbol2, c.Symbol3, c.IsArchived))
                .ToArrayAsync(cancellationToken)
        };
    }
}

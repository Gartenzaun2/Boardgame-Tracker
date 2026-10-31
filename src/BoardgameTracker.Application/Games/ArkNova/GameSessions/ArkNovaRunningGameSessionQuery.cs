using BoardgameTracker.Application.Games.ArkNova.Content;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Games.ArkNova.GameSessions;

/// <summary>
/// EF Core projection for the running Ark Nova screen, mapped to Application read models after provider-side materialization.
/// </summary>
public sealed class ArkNovaRunningGameSessionQuery(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory, ArkNovaContent content)
{
    public async Task<ArkNovaRunningGameSessionData?> GetRunningGameSessionAsync(
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

        IReadOnlyDictionary<int, string> zooPlanNames = await content.ReadZooPlanNamesAsync(dbContext, cancellationToken);
        IReadOnlyList<ArkNovaRunningGameSessionPlayer> players =
        [
            .. playerRows.Select(player => new ArkNovaRunningGameSessionPlayer(
                player.PlayerId,
                player.Name,
                player.Order,
                player.Color,
                player.ZooPlanNumber, zooPlanNames[player.ZooPlanNumber]))
        ];

        return new ArkNovaRunningGameSessionData(
            gameSessionRow.GameSessionId,
            gameSessionRow.GroupName,
            gameSessionRow.StartedAt,
            players);
    }
}

/// <summary>
/// Screen-oriented Application read model for one running Ark Nova gameSession. It is
/// deliberately separate from the Core GameSession aggregate because the screen
/// also needs projected group, player and zoo-plan display data.
/// </summary>
public sealed record ArkNovaRunningGameSessionData(
    Guid GameSessionId,
    string GroupName,
    DateTimeOffset StartedAt,
    IReadOnlyList<ArkNovaRunningGameSessionPlayer> Players);

/// <summary>
/// Projected player row for the running GameSession screen. This is not the persisted
/// ArkNovaGameSessionPlayer entity; it combines gameSession state with player and zoo-plan
/// display data needed by the UI.
/// </summary>
public sealed record ArkNovaRunningGameSessionPlayer(
    Guid PlayerId,
    string Name,
    int Order,
    PlayerColor Color,
    int ZooPlanNumber,
    string ZooPlanName);

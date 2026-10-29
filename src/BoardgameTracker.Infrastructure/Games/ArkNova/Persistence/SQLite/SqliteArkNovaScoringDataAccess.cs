using BoardgameTracker.Application.Games.ArkNova.SpecialCards;
using BoardgameTracker.Application.Games.ArkNova.GameSessions.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using BoardgameTracker.Core.Games.ArkNova.GameSessions;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Infrastructure.Persistence.SQLite;

namespace BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite;

/// <summary>
/// SQLite persistence adapter for Ark Nova scoring. A fresh DbContext is used for each operation.
/// </summary>
public sealed class SqliteArkNovaScoringDataAccess(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IArkNovaScoringDataAccess
{
    public async Task<ArkNovaScoringGameSessionData?> LoadAsync(
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
            select new ArkNovaScoringPlayerData(
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

        return new ArkNovaScoringGameSessionData(
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

    public async Task<ArkNovaGameSessionPlayer?> FindPlayerAsync(
        Guid gameSessionId,
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.ArkNovaGameSessionPlayers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.GameSessionId == gameSessionId && x.PlayerId == playerId,
                cancellationToken);
    }

    public async Task SavePlayerAsync(ArkNovaGameSessionPlayer player, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.Attach(player);
        dbContext.Entry(player).Property(x => x.Attractiveness).IsModified = true;
        dbContext.Entry(player).Property(x => x.Conservation).IsModified = true;
        dbContext.Entry(player).Property(x => x.Reputation).IsModified = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GameSession?> FindGameSessionAsync(Guid gameSessionId, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.GameSessions
            .AsNoTracking()
            .Include(x => x.Players)
            .SingleOrDefaultAsync(x => x.Id == gameSessionId, cancellationToken);
    }

    public async Task SaveCompletedGameSessionAsync(GameSession gameSession, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gameSession);
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.Attach(gameSession);
        dbContext.Entry(gameSession).Property(x => x.Status).IsModified = true;
        dbContext.Entry(gameSession).Property(x => x.EndedAt).IsModified = true;
        dbContext.Entry(gameSession).Property(x => x.ModifiedAt).IsModified = true;
        foreach (GameSessionPlayer player in gameSession.Players)
        {
            dbContext.Entry(player).Property(x => x.Placement).IsModified = true;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddSpecialCardAsync(
        ArkNovaGameSessionSpecialCardAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (!await dbContext.ArkNovaSpecialCards.AnyAsync(c => c.Id == assignment.SpecialCardId && !c.IsArchived, cancellationToken))
            throw new InvalidOperationException("Only active special cards may be assigned.");
        bool exists = await dbContext.ArkNovaGameSessionSpecialCardAssignments.AnyAsync(
            x => x.GameSessionId == assignment.GameSessionId
                 && x.PlayerId == assignment.PlayerId
                 && x.SpecialCardId == assignment.SpecialCardId,
            cancellationToken);

        if (!exists)
        {
            dbContext.ArkNovaGameSessionSpecialCardAssignments.Add(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RemoveSpecialCardAsync(
        Guid gameSessionId,
        Guid playerId,
        Guid specialCardId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        ArkNovaGameSessionSpecialCardAssignment? assignment = await dbContext.ArkNovaGameSessionSpecialCardAssignments
            .SingleOrDefaultAsync(
                x => x.GameSessionId == gameSessionId && x.PlayerId == playerId && x.SpecialCardId == specialCardId,
                cancellationToken);

        if (assignment is not null)
        {
            dbContext.ArkNovaGameSessionSpecialCardAssignments.Remove(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteGameSessionAsync(
        Guid gameSessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        GameSession? gameSession = await dbContext.GameSessions
            .SingleOrDefaultAsync(x => x.Id == gameSessionId, cancellationToken);

        if (gameSession is not null)
        {
            dbContext.GameSessions.Remove(gameSession);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

}

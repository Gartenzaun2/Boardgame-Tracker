using BoardgameTracker.Application.Games.ArkNova.Scoring;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ArkNova;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Games.ArkNova;

/// <summary>
/// SQLite persistence adapter for Ark Nova scoring. A fresh DbContext is used for each operation.
/// </summary>
public sealed class SqliteArkNovaScoringDataAccess(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
    : IArkNovaScoringDataAccess
{
    public async Task<ArkNovaScoringSessionData?> LoadAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var session = await dbContext.GameSessions
            .AsNoTracking()
            .Where(x => x.Id == sessionId)
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.StartedAt,
                x.EndedAt,
                GroupName = dbContext.Groups.Where(g => g.Id == x.GroupId).Select(g => g.Name).Single()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return null;
        }

        var players = await (
            from gamePlayer in dbContext.GameSessionPlayers.AsNoTracking()
            join arkPlayer in dbContext.ArkNovaSessionPlayers.AsNoTracking()
                on new { gamePlayer.GameSessionId, gamePlayer.PersonId }
                equals new { arkPlayer.GameSessionId, arkPlayer.PersonId }
            join person in dbContext.Persons.AsNoTracking() on gamePlayer.PersonId equals person.Id
            where gamePlayer.GameSessionId == sessionId
            orderby gamePlayer.Order
            select new ArkNovaScoringPlayerData(
                gamePlayer.PersonId,
                person.Name,
                gamePlayer.Order,
                arkPlayer.Color,
                arkPlayer.SelectedZooPlanNumber,
                arkPlayer.Attractiveness,
                arkPlayer.Conservation,
                arkPlayer.Reputation,
                gamePlayer.Placement))
            .ToListAsync(cancellationToken);

        List<ArkNovaSpecialCardAssignmentData> specialCards = await dbContext.ArkNovaSessionSpecialCardAssignments
            .AsNoTracking()
            .Where(x => x.GameSessionId == sessionId)
            .Select(x => new ArkNovaSpecialCardAssignmentData(x.PersonId, x.SpecialCard))
            .ToListAsync(cancellationToken);

        return new ArkNovaScoringSessionData(
            session.Id,
            session.GroupName,
            session.Status,
            session.StartedAt,
            session.EndedAt,
            players,
            specialCards);
    }

    public async Task<ArkNovaSessionPlayer?> FindPlayerAsync(
        Guid sessionId,
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.ArkNovaSessionPlayers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.GameSessionId == sessionId && x.PersonId == personId,
                cancellationToken);
    }

    public async Task SavePlayerAsync(ArkNovaSessionPlayer player, CancellationToken cancellationToken = default)
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

    public async Task<GameSession?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.GameSessions
            .AsNoTracking()
            .Include(x => x.Players)
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
    }

    public async Task SaveCompletedSessionAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.Attach(session);
        dbContext.Entry(session).Property(x => x.Status).IsModified = true;
        dbContext.Entry(session).Property(x => x.EndedAt).IsModified = true;
        dbContext.Entry(session).Property(x => x.ModifiedAt).IsModified = true;
        foreach (GameSessionPlayer player in session.Players)
        {
            dbContext.Entry(player).Property(x => x.Placement).IsModified = true;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddSpecialCardAsync(
        ArkNovaSessionSpecialCardAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        bool exists = await dbContext.ArkNovaSessionSpecialCardAssignments.AnyAsync(
            x => x.GameSessionId == assignment.GameSessionId
                 && x.PersonId == assignment.PersonId
                 && x.SpecialCard == assignment.SpecialCard,
            cancellationToken);

        if (!exists)
        {
            dbContext.ArkNovaSessionSpecialCardAssignments.Add(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveSpecialCardAsync(
        Guid sessionId,
        Guid personId,
        ArkNovaSpecialCard specialCard,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        ArkNovaSessionSpecialCardAssignment? assignment = await dbContext.ArkNovaSessionSpecialCardAssignments
            .SingleOrDefaultAsync(
                x => x.GameSessionId == sessionId && x.PersonId == personId && x.SpecialCard == specialCard,
                cancellationToken);

        if (assignment is not null)
        {
            dbContext.ArkNovaSessionSpecialCardAssignments.Remove(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        GameSession? session = await dbContext.GameSessions
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is not null)
        {
            dbContext.GameSessions.Remove(session);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

}

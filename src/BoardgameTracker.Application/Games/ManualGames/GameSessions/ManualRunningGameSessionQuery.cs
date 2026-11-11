using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games.ManualGames;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ManualGames.GameSessions;

public sealed class ManualRunningGameSessionQuery(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task<Guid?> FindOpenAsync(string gameKey, Guid groupId, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        return await (from session in db.GameSessions.AsNoTracking()
                      join game in db.Games.AsNoTracking() on session.GameId equals game.Id
                      join manual in db.ManualGameSessions.AsNoTracking() on session.Id equals manual.GameSessionId
                      where game.Key == gameKey && session.GroupId == groupId
                          && (session.Status == GameSessionStatus.Running || session.Status == GameSessionStatus.Scoring)
                      select (Guid?)session.Id).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ManualRunningData?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        return await ReadAsync(db, sessionId, cancellationToken);
    }

    internal static async Task<ManualRunningData?> ReadAsync(BoardgameTrackerDbContext db, Guid sessionId, CancellationToken cancellationToken)
    {
        var data = await (from session in db.GameSessions.AsNoTracking()
                          join manual in db.ManualGameSessions.AsNoTracking() on session.Id equals manual.GameSessionId
                          join game in db.Games.AsNoTracking() on session.GameId equals game.Id
                          join groupRow in db.Groups.AsNoTracking() on session.GroupId equals groupRow.Id
                          where session.Id == sessionId
                          select new { session.Id, session.Status, session.StartedAt, session.EndedAt, GameKey = game.Key,
                              GameName = game.Name, game.BadgeText, GroupName = groupRow.Name, manual.ScoringRulesSnapshot }).SingleOrDefaultAsync(cancellationToken);
        if (data is null)
            return null;
        ManualRunningPlayer[] players = await (from participant in db.GameSessionPlayers.AsNoTracking()
            join player in db.Players.AsNoTracking() on participant.PlayerId equals player.Id
            join color in db.GameSessionPlayerColors.AsNoTracking()
                on new { participant.GameSessionId, participant.PlayerId } equals new { color.GameSessionId, color.PlayerId } into colors
            from color in colors.DefaultIfEmpty()
            where participant.GameSessionId == sessionId
            select new ManualRunningPlayer(player.Id, player.Name, participant.Order, color == null ? (PlayerColor?)null : color.Color))
            .ToArrayAsync(cancellationToken);
        return new(data.Id, data.GameKey, data.GameName, data.BadgeText, data.GroupName, data.Status, data.StartedAt, data.EndedAt,
            data.ScoringRulesSnapshot, players.OrderBy(player => player.Order).ThenBy(player => player.Name, StringComparer.CurrentCulture).ToArray());
    }
}

public sealed record ManualRunningData(Guid SessionId, string GameKey, string GameName, string BadgeText, string GroupName,
    GameSessionStatus Status, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, ScoringRulesSnapshot ScoringRules,
    IReadOnlyList<ManualRunningPlayer> Players);
public sealed record ManualRunningPlayer(Guid PlayerId, string Name, int? Order, PlayerColor? Color);

public sealed class ManualRunningGameSessionUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task BeginScoringAsync(Guid sessionId, DateTimeOffset endedAt, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        GameSession session = await GetOpenAsync(db, sessionId, cancellationToken);
        if (session.Status == GameSessionStatus.Running)
        {
            session.BeginScoring(endedAt);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DiscardAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        GameSession session = await GetOpenAsync(db, sessionId, cancellationToken);
        db.GameSessions.Remove(session);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<GameSession> GetOpenAsync(BoardgameTrackerDbContext db, Guid id, CancellationToken token)
    {
        if (!await db.ManualGameSessions.AnyAsync(session => session.GameSessionId == id, token))
            throw new InvalidOperationException("The manual session does not exist.");
        GameSession session = await db.GameSessions.SingleAsync(session => session.Id == id, token);
        if (session.Status is not (GameSessionStatus.Running or GameSessionStatus.Scoring))
            throw new InvalidOperationException("Only an open manual session can be changed here.");
        return session;
    }
}

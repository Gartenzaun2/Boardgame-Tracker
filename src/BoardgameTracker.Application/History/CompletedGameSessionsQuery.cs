using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;
namespace BoardgameTracker.Application.History;

public sealed class CompletedGameSessionsQuery(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task<IReadOnlyList<HistoryGameSession>> ListAsync(string? gameKey = null, Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var sessions = await (from s in db.GameSessions.AsNoTracking()
                              join g in db.Games on s.GameId equals g.Id
                              join grp in db.Groups on s.GroupId equals grp.Id
                              where s.Status == GameSessionStatus.Completed && (gameKey == null || g.Key == gameKey)
                                  && (groupId == null || s.GroupId == groupId)
                              select new { s.Id, g.Key, s.GroupId, GroupName = grp.Name, s.StartedAt, s.EndedAt }).ToListAsync(cancellationToken);
        var ids = sessions.Select(s => s.Id).ToArray();
        var players = await (from sp in db.GameSessionPlayers.AsNoTracking()
                             join p in db.Players on sp.PlayerId equals p.Id
                             where ids.Contains(sp.GameSessionId)
                             select new { sp.GameSessionId, sp.PlayerId, p.Name, sp.Placement, sp.Order }).ToListAsync(cancellationToken);
        // SQLite cannot translate DateTimeOffset ordering. Keep the minimal local projection
        // and sort it in memory; changing persisted timestamp types is not warranted here.
        return sessions.OrderByDescending(s => s.StartedAt).ThenBy(s => s.Id).Select(s => new HistoryGameSession(
            s.Id, s.Key, s.GroupId, s.GroupName, s.StartedAt, s.EndedAt,
            players.Where(p => p.GameSessionId == s.Id).OrderBy(p => p.Placement).ThenBy(p => p.Order)
                .Select(p => new HistoryPlayer(p.PlayerId, p.Name, p.Placement ?? 0)).ToArray())).ToArray();
    }
}

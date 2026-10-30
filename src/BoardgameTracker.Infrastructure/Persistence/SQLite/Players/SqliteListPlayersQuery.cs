using BoardgameTracker.Application.Players.ListPlayers;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Persistence.SQLite.Players;

public sealed class SqliteListPlayersQuery(IDbContextFactory<BoardgameTrackerDbContext> factory) : IListPlayersQuery
{
    public async Task<IReadOnlyList<PlayerListItem>> ListPlayersAsync(bool includeGuests = false, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        List<PlayerListItem> players = await db.Players.AsNoTracking()
            .Where(player => (includeGuests || !player.IsGuest) && (includeArchived || !player.IsArchived))
            .Select(player => new PlayerListItem(player.Id, player.Name, player.IsGuest, player.IsArchived, player.CreatedAt, null, null, null))
            .ToListAsync(cancellationToken);
        if (players.Count == 0)
            return [];

        Guid[] ids = players.Select(player => player.PlayerId).ToArray();
        var sessions = await (from participant in db.GameSessionPlayers.AsNoTracking()
                              join session in db.GameSessions.AsNoTracking() on participant.GameSessionId equals session.Id
                              join game in db.Games.AsNoTracking() on session.GameId equals game.Id
                              where ids.Contains(participant.PlayerId)
                              select new { participant.PlayerId, session.Id, session.StartedAt, game.Key }).ToListAsync(cancellationToken);
        // SQLite cannot order DateTimeOffset directly. Use the same date semantics as the group overview.
        var latest = sessions.GroupBy(session => session.PlayerId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(session => session.StartedAt).ThenBy(session => session.Id).First());
        return players.Select(player => latest.TryGetValue(player.PlayerId, out var session)
                ? player with { LastPlayedAt = session.StartedAt, LastGameSessionId = session.Id, LastGameKey = session.Key }
                : player)
            .OrderBy(player => player.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(player => player.PlayerId).ToArray();
    }
}

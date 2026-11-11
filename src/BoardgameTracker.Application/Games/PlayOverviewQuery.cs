using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games;

public sealed class PlayOverviewQuery(IDbContextFactory<BoardgameTrackerDbContext> factory, IGameActivationStore activationStore)
{
    public async Task<GameDetails> GetGameAsync(string gameKey, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Games.AsNoTracking().Where(game => game.Key == gameKey)
            .Select(game => new GameDetails(game.Name, game.BadgeText)).SingleAsync(cancellationToken);
    }

    public async Task<string?> GetSingleOpenGameKeyAsync(CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        string[] keys = await (from session in db.GameSessions.AsNoTracking()
                              join game in db.Games.AsNoTracking() on session.GameId equals game.Id
                              where session.Status == GameSessionStatus.Running || session.Status == GameSessionStatus.Scoring
                              select game.Key).Distinct().Take(2).ToArrayAsync(cancellationToken);
        return keys.Length == 1 ? keys[0] : null;
    }
    public async Task<IReadOnlyList<PlayGame>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        var games = await db.Games.AsNoTracking().Select(game => new { game.Id, game.Key, game.Name, game.BadgeText }).ToArrayAsync(cancellationToken);
        var sessions = await db.GameSessions.AsNoTracking().Select(session => new { session.GameId, session.Status, session.StartedAt }).ToArrayAsync(cancellationToken);
        return games.Where(game => activationStore.IsEnabled(game.Key)
                || sessions.Any(session => session.GameId == game.Id && session.Status is GameSessionStatus.Running or GameSessionStatus.Scoring))
            .Select(game => new PlayGame(game.Id, game.Key, game.Name, game.BadgeText,
            sessions.Any(session => session.GameId == game.Id && session.Status is GameSessionStatus.Running or GameSessionStatus.Scoring),
            sessions.Where(session => session.GameId == game.Id).Select(session => (DateTimeOffset?)session.StartedAt).Max()))
            .OrderByDescending(game => game.HasOpenSession).ThenByDescending(game => game.LastStartedAt).ThenBy(game => game.Key, StringComparer.Ordinal).ToArray();
    }
}
public sealed record PlayGame(Guid Id, string Key, string Name, string BadgeText, bool HasOpenSession, DateTimeOffset? LastStartedAt);
public sealed record GameDetails(string Name, string BadgeText);

using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games;

/// <summary>Reads shared game metadata and effective enablement for settings and regular game selections.</summary>
public sealed class GamesQuery(IDbContextFactory<BoardgameTrackerDbContext> factory, IGameEnabledStore enabledStore) {
    public Task<IReadOnlyList<GameInfo>> GetAllAsync(CancellationToken cancellationToken = default)
        => ReadGamesAsync(null, cancellationToken);

    public async Task<IReadOnlyList<GameInfo>> GetEnabledAsync(CancellationToken cancellationToken = default)
        => (await GetAllAsync(cancellationToken)).Where(game => game.IsEnabled).ToArray();

    public async Task<GameInfo> GetAsync(string gameKey, CancellationToken cancellationToken = default)
        => (await ReadGamesAsync(gameKey, cancellationToken)).Single(game => game.Key == gameKey);

    private async Task<IReadOnlyList<GameInfo>> ReadGamesAsync(string? gameKey, CancellationToken cancellationToken) {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        var games = await db.Games.AsNoTracking().Where(game => gameKey == null || game.Key == gameKey).OrderBy(game => game.Key)
            .Select(game => new {
                game.Id, game.Key, game.Name, game.BadgeText, game.MinPlayers, game.MaxPlayers,
                HasOpenSessions = db.GameSessions.Any(session => session.GameId == game.Id
                    && (session.Status == GameSessionStatus.Running || session.Status == GameSessionStatus.Scoring))
            }).ToArrayAsync(cancellationToken);

        // An open session stays reachable even if an opt-out predates its start.
        return games.Select(game => new GameInfo(game.Id, game.Key, game.Name, game.BadgeText, game.MinPlayers, game.MaxPlayers,
            game.HasOpenSessions || enabledStore.IsEnabled(game.Key), game.HasOpenSessions)).ToArray();
    }
}

public sealed record GameInfo(Guid Id, string Key, string Name, string BadgeText, int MinimumPlayers, int MaximumPlayers, bool IsEnabled, bool HasOpenSessions);

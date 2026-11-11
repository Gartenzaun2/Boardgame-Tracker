using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Settings;

public sealed class GameActivationUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory, IGameActivationStore store)
{
    public async Task<IReadOnlyList<GameActivationState>> GetGamesAsync(CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        var games = await db.Games.AsNoTracking().OrderBy(game => game.Key)
            .Select(game => new
            {
                game.Key,
                game.Name,
                HasOpenSessions = db.GameSessions.Any(session => session.GameId == game.Id
                    && (session.Status == GameSessionStatus.Running || session.Status == GameSessionStatus.Scoring))
            }).ToArrayAsync(cancellationToken);
        // An open session stays reachable even if an opt-out predates its start.
        return games.Select(game => new GameActivationState(game.Key,
            game.HasOpenSessions || store.IsEnabled(game.Key), game.HasOpenSessions, game.Name)).ToArray();
    }

    public async Task<GameActivationState> SetEnabledAsync(string gameKey, bool enabled, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        // Keep the current session check protected through the preference write.
        // This transaction only reads SQLite; disposing it makes no database changes.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var game = await db.Games.Where(game => game.Key == gameKey).Select(game => new { game.Id, game.Name }).SingleOrDefaultAsync(cancellationToken);
        if (game is null)
            throw new InvalidOperationException("The game does not exist.");
        bool hasOpenSessions = await db.GameSessions.AnyAsync(session => session.GameId == game.Id
            && (session.Status == GameSessionStatus.Running || session.Status == GameSessionStatus.Scoring), cancellationToken);
        if (!enabled && hasOpenSessions)
            throw new GameActivationBlockedException();
        store.SetEnabled(gameKey, enabled);
        return new(gameKey, enabled || hasOpenSessions, hasOpenSessions, game.Name);
    }
}

public sealed record GameActivationState(string GameKey, bool IsEnabled, bool HasOpenSessions, string Name);
public sealed class GameActivationBlockedException() : InvalidOperationException("A game with an open session cannot be disabled.");

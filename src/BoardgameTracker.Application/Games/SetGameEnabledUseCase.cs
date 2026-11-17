using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games;

public sealed class SetGameEnabledUseCase(IDbContextFactory<BoardgameTrackerDbContext> factory, IGameEnabledStore store) {
    public async Task<GameEnabledState> SetEnabledAsync(string gameKey, bool enabled, CancellationToken cancellationToken = default) {
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
            throw new GameEnablementBlockedException();
        store.SetEnabled(gameKey, enabled);
        return new(gameKey, enabled || hasOpenSessions, hasOpenSessions, game.Name);
    }
}

public sealed record GameEnabledState(string GameKey, bool IsEnabled, bool HasOpenSessions, string Name);
public sealed class GameEnablementBlockedException() : InvalidOperationException("A game with an open session cannot be disabled.");

using BoardgameTracker.Core.Games;

namespace BoardgameTracker.Application.Groups;

public sealed class GamePlayerLimitCatalog(IEnumerable<GamePlayerLimit> limits)
{
    private readonly IReadOnlyDictionary<string, GamePlayerLimit> _limits =
        limits.ToDictionary(limit => limit.GameKey, StringComparer.Ordinal);

    public int GetMaximumPlayers(string gameKey) => GetLimit(gameKey).MaximumPlayers;

    public GamePlayerLimit GetLimit(string gameKey) => _limits.TryGetValue(gameKey, out GamePlayerLimit? limit)
        ? limit
        : throw new InvalidOperationException("No player limit is registered for this game.");
}

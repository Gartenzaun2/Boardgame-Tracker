using BoardgameTracker.Core.Games;

namespace BoardgameTracker.Application.Groups;

public sealed class GamePlayerLimitCatalog(IEnumerable<GamePlayerLimit> limits)
{
    private readonly IReadOnlyDictionary<string, GamePlayerLimit> _limits =
        limits.ToDictionary(limit => limit.GameKey, StringComparer.Ordinal);

    public int GetMaximumPlayers(string gameKey) => _limits.TryGetValue(gameKey, out GamePlayerLimit? limit)
        ? limit.MaximumPlayers
        : throw new InvalidOperationException("No player limit is registered for this game.");
}

public sealed class GroupPlayerLimitExceededException(string gameKey, int maximumPlayers)
    : InvalidOperationException("The group exceeds the game's maximum player count.")
{
    public string GameKey { get; } = gameKey;
    public int MaximumPlayers { get; } = maximumPlayers;
}

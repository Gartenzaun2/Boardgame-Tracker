namespace BoardgameTracker.Core.Games;

/// <summary>Immutable maximum membership supported by a game.</summary>
public sealed class GamePlayerLimit
{
    public GamePlayerLimit(string gameKey, int maximumPlayers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumPlayers, 1);
        GameKey = gameKey;
        MaximumPlayers = maximumPlayers;
    }

    public string GameKey { get; }
    public int MaximumPlayers { get; }
}

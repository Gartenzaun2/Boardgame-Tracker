namespace BoardgameTracker.Core.Groups;

public sealed class GroupPlayerLimitExceededException(string gameKey, int maximumPlayers)
    : InvalidOperationException("The group exceeds the game's maximum player count.")
{
    public string GameKey { get; } = gameKey;
    public int MaximumPlayers { get; } = maximumPlayers;
}

public sealed class GroupPlayerMinimumNotMetException(string gameKey, int minimumPlayers)
    : InvalidOperationException("The group does not meet the game's minimum player count.")
{
    public string GameKey { get; } = gameKey;
    public int MinimumPlayers { get; } = minimumPlayers;
}

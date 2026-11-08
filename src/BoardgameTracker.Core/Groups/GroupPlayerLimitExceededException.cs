namespace BoardgameTracker.Core.Groups;

public sealed class GroupPlayerLimitExceededException(string gameKey, int maximumPlayers)
    : InvalidOperationException("The group exceeds the game's maximum player count.")
{
    public string GameKey { get; } = gameKey;
    public int MaximumPlayers { get; } = maximumPlayers;
}

namespace BoardgameTracker.Core.GameSessions;

public sealed class Game
{
    private Game()
    {
    }

    public Game(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Id = Guid.NewGuid();
        Key = key.Trim();
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;
}

namespace BoardgameTracker.Core.Games;

/// <summary>
/// Represents one supported board game using a stable, language-independent key.
/// </summary>
public sealed class Game
{
    private Game()
    {
    }

    public Game(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Id = Guid.CreateVersion7();
        Key = key.Trim();
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;
}

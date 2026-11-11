namespace BoardgameTracker.Core.Games;

/// <summary>
/// Represents one supported board game using a stable, language-independent key.
/// </summary>
public sealed class Game
{
    private Game()
    {
    }

    public Game(string key, string name, string badgeText, int minPlayers, int maxPlayers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Trim().Length > 100)
            throw new ArgumentException("The game key must not exceed 100 characters.", nameof(key));
        SetMetadata(name, badgeText, minPlayers, maxPlayers);
        Id = Guid.CreateVersion7();
        Key = key.Trim();
    }

    /// <summary>Updates shared metadata without changing the game's stable identity.</summary>
    public void SetMetadata(string name, string badgeText, int minPlayers, int maxPlayers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(badgeText);
        if (name.Trim().Length > 100)
            throw new ArgumentException("The game name must not exceed 100 characters.", nameof(name));
        ArgumentOutOfRangeException.ThrowIfLessThan(minPlayers, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPlayers, minPlayers);
        Name = name.Trim();
        BadgeText = badgeText.Trim();
        MinPlayers = minPlayers;
        MaxPlayers = maxPlayers;
    }

    public bool SupportsPlayerCount(int playerCount) => playerCount >= MinPlayers && playerCount <= MaxPlayers;

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string BadgeText { get; private set; } = string.Empty;
    public int MinPlayers { get; private set; }
    public int MaxPlayers { get; private set; }
}

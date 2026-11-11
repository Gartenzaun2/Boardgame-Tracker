using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Core.GameSessions;

/// <summary>Historical color snapshot, independent of subsequent group default changes.</summary>
public sealed class GameSessionPlayerColor
{
    private GameSessionPlayerColor() { }

    public GameSessionPlayerColor(Guid gameSessionId, Guid playerId, PlayerColor color)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        if (!Enum.IsDefined(color))
            throw new ArgumentOutOfRangeException(nameof(color));
        GameSessionId = gameSessionId;
        PlayerId = playerId;
        Color = color;
    }

    public Guid GameSessionId { get; private set; }
    public Guid PlayerId { get; private set; }
    public PlayerColor Color { get; private set; }

    public static IReadOnlyList<GameSessionPlayerColor> Create(GameSession gameSession, IReadOnlyDictionary<Guid, PlayerColor> colors)
    {
        ArgumentNullException.ThrowIfNull(gameSession);
        ArgumentNullException.ThrowIfNull(colors);
        if (colors.Keys.Any(id => !gameSession.Players.Any(player => player.PlayerId == id)))
            throw new ArgumentException("Only session participants may receive colors.", nameof(colors));
        if (colors.Values.Distinct().Count() != colors.Count)
            throw new ArgumentException("Session colors must be unique.", nameof(colors));
        return colors.Select(pair => new GameSessionPlayerColor(gameSession.Id, pair.Key, pair.Value)).ToArray();
    }
}

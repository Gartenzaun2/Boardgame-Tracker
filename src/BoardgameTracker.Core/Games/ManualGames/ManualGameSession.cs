namespace BoardgameTracker.Core.Games.ManualGames;

/// <summary>Immutable scoring contract captured when a manual session starts.</summary>
public sealed record ScoringRulesSnapshot(
    bool CapturePoints, bool AllowNegativePoints, bool CaptureWinner, bool CapturePlacements,
    bool AllowTies, bool ResultFromPoints, PointsDirection? PointsDirection);

public sealed class ManualGameSession
{
    private ManualGameSession() { }
    public ManualGameSession(Guid gameSessionId, ManualGame game)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(game);
        GameSessionId = gameSessionId;
        ScoringRulesSnapshot = new(game.CapturePoints, game.AllowNegativePoints, game.CaptureWinner,
            game.CapturePlacements, game.AllowTies, game.ResultFromPoints, game.PointsDirection);
    }
    public Guid GameSessionId { get; private set; }
    public ScoringRulesSnapshot ScoringRulesSnapshot { get; private set; } = null!;
}

/// <summary>Minimal historical points storage for reading the last completed session; no scoring workflow yet.</summary>
public sealed class ManualGameSessionPlayer
{
    private ManualGameSessionPlayer() { }
    public ManualGameSessionPlayer(Guid gameSessionId, Guid playerId, int? points = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        GameSessionId = gameSessionId;
        PlayerId = playerId;
        Points = points;
    }
    public Guid GameSessionId { get; private set; }
    public Guid PlayerId { get; private set; }
    public int? Points { get; private set; }
}

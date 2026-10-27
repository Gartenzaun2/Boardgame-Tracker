namespace BoardgameTracker.Core.GameSessions;

/// <summary>
/// Defines the persisted lifecycle states of a game session.
/// </summary>
public enum GameSessionStatus
{
    Running = 1,
    Scoring = 2,
    Completed = 3
}

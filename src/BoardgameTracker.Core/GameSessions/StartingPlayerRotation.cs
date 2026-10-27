namespace BoardgameTracker.Core.GameSessions;

/// <summary>
/// Provides the pure cyclic rotation used to move a selected player to position one without changing relative seating order.
/// </summary>
public static class StartingPlayerRotation
{
    public static IReadOnlyList<T> RotateToStart<T>(
        IReadOnlyList<T> players,
        int startIndex)
    {
        ArgumentNullException.ThrowIfNull(players);

        if (players.Count == 0)
        {
            return [];
        }

        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);

        if (startIndex >= players.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startIndex),
                startIndex,
                "The start index must refer to an existing player.");
        }

        if (startIndex == 0)
        {
            return [.. players];
        }

        T[] result = new T[players.Count];

        for (int targetIndex = 0; targetIndex < players.Count; targetIndex++)
        {
            int sourceIndex = (startIndex + targetIndex) % players.Count;
            result[targetIndex] = players[sourceIndex];
        }

        return result;
    }
}

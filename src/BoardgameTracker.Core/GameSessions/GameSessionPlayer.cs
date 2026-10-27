using BoardgameTracker.Core.Players;
namespace BoardgameTracker.Core.GameSessions;

/// <summary>
/// Represents one participant snapshot inside a GameSession aggregate, including order and optional final placement.
/// </summary>
public sealed class GameSessionPlayer
{
    private GameSessionPlayer()
    {
    }

    internal GameSessionPlayer(Guid gameSessionId, Guid playerId, int order)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order);

        GameSessionId = gameSessionId;
        PlayerId = playerId;
        Order = order;
    }

    public Guid GameSessionId { get; private set; }
    public Guid PlayerId { get; private set; }
    public int Order { get; private set; }
    public int? Placement { get; private set; }

    public void SetPlacement(int? placement)
    {
        if (placement is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placement));
        }

        Placement = placement;
    }
}

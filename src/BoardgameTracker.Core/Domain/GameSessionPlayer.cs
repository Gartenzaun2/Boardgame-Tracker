namespace BoardgameTracker.Core.Domain;

public sealed class GameSessionPlayer
{
    private GameSessionPlayer()
    {
    }

    public GameSessionPlayer(Guid gameSessionId, Guid personId, int order)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameSessionId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(personId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order);

        GameSessionId = gameSessionId;
        PersonId = personId;
        Order = order;
    }

    public Guid GameSessionId { get; private set; }

    public GameSession? GameSession { get; private set; }

    public Guid PersonId { get; private set; }

    public Person? Person { get; private set; }

    public int Order { get; private set; }

    public int? Placement { get; private set; }

    public void SetOrder(int order)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(order);
        Order = order;
    }

    public void SetPlacement(int? placement)
    {
        if (placement is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placement));
        }

        Placement = placement;
    }
}

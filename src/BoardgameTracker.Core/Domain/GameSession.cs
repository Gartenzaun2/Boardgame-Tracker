namespace BoardgameTracker.Core.Domain;

public sealed class GameSession
{
    private readonly List<GameSessionPlayer> _players = [];

    private GameSession()
    {
    }

    public GameSession(Guid gameId, Guid groupId, DateTimeOffset startedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);

        Id = Guid.NewGuid();
        GameId = gameId;
        GroupId = groupId;
        Status = GameSessionStatus.Running;
        StartedAt = startedAt;
        ModifiedAt = startedAt;
    }

    public Guid Id { get; private set; }

    public Guid GameId { get; private set; }

    public Game? Game { get; private set; }

    public Guid GroupId { get; private set; }

    public Group? Group { get; private set; }

    public GameSessionStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public DateTimeOffset ModifiedAt { get; private set; }

    public IReadOnlyCollection<GameSessionPlayer> Players => _players;

    public void BeginScoring(DateTimeOffset modifiedAt)
    {
        Status = GameSessionStatus.Scoring;
        ModifiedAt = modifiedAt;
    }

    public void Complete(DateTimeOffset endedAt)
    {
        Status = GameSessionStatus.Completed;
        EndedAt = endedAt;
        ModifiedAt = endedAt;
    }
}

using BoardgameTracker.Core.Players;
namespace BoardgameTracker.Core.GameSessions;

/// <summary>
/// Aggregate root for a persisted game gameSession. It owns participants and protects
/// the GameSession lifecycle from Running through Scoring to Completed.
/// </summary>
public sealed class GameSession
{
    private readonly List<GameSessionPlayer> _players = [];

    private GameSession()
    {
    }

    public GameSession(
        Guid gameId,
        Guid groupId,
        DateTimeOffset startedAt,
        IReadOnlyList<Guid> playerIds, Guid? id = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(gameId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(playerIds);

        if (playerIds.Count == 0)
        {
            throw new ArgumentException(
                "A game gameSession must contain at least one player.",
                nameof(playerIds));
        }

        if (playerIds.Any(id => id == Guid.Empty)
            || playerIds.Distinct().Count() != playerIds.Count)
        {
            throw new ArgumentException(
                "Every player must occur exactly once.",
                nameof(playerIds));
        }

        Id = id ?? Guid.CreateVersion7();
        ArgumentOutOfRangeException.ThrowIfEqual(Id, Guid.Empty);
        GameId = gameId;
        GroupId = groupId;
        Status = GameSessionStatus.Running;
        StartedAt = startedAt;
        ModifiedAt = startedAt;

        for (int index = 0; index < playerIds.Count; index++)
        {
            _players.Add(new GameSessionPlayer(Id, playerIds[index], index + 1));
        }
    }

    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public Guid GroupId { get; private set; }
    public GameSessionStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public DateTimeOffset ModifiedAt { get; private set; }
    public IReadOnlyCollection<GameSessionPlayer> Players => _players;

    public void BeginScoring(DateTimeOffset modifiedAt)
    {
        if (Status != GameSessionStatus.Running)
        {
            throw new InvalidOperationException(
                "Only a running GameSession can enter scoring.");
        }

        if (modifiedAt < StartedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(modifiedAt));
        }

        Status = GameSessionStatus.Scoring;
        EndedAt = modifiedAt;
        ModifiedAt = modifiedAt;
    }

    public void Complete(DateTimeOffset modifiedAt)
    {
        if (Status != GameSessionStatus.Scoring)
        {
            throw new InvalidOperationException(
                "Only a GameSession in scoring can be completed.");
        }

        if (modifiedAt < StartedAt
            || EndedAt is DateTimeOffset endedAt && modifiedAt < endedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(modifiedAt));
        }

        Status = GameSessionStatus.Completed;
        EndedAt ??= modifiedAt;
        ModifiedAt = modifiedAt;
    }
}

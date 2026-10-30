namespace BoardgameTracker.Application.History;

public interface ICompletedGameSessionsQuery
{
    Task<IReadOnlyList<HistoryGameSession>> ListAsync(string? gameKey = null, Guid? groupId = null, CancellationToken cancellationToken = default);
}
public sealed record HistoryPlayer(Guid PlayerId, string Name, int Placement);
public sealed record HistoryGameSession(Guid GameSessionId, string GameKey, Guid GroupId, string GroupName,
    DateTimeOffset StartedAt, DateTimeOffset? EndedAt, IReadOnlyList<HistoryPlayer> Players);

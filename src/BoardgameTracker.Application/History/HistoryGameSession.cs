namespace BoardgameTracker.Application.History;

public sealed record HistoryPlayer(Guid PlayerId, string Name, int Placement);
public sealed record HistoryGameSession(Guid GameSessionId, string GameKey, Guid GroupId, string GroupName,
    DateTimeOffset StartedAt, DateTimeOffset? EndedAt, IReadOnlyList<HistoryPlayer> Players);

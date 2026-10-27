using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.Groups.ListGroups;

/// <summary>
/// Screen-oriented Application read model for one group in the group overview.
/// </summary>
public sealed record GroupListItem(
    Guid Id,
    string Name,
    bool IsTemporary,
    bool IsArchived,
    int PlayerCount,
    IReadOnlyList<string> PlayerNames,
    DateTimeOffset? LastPlayedAt,
    Guid? OpenGameSessionId,
    GameSessionStatus? OpenGameSessionStatus);

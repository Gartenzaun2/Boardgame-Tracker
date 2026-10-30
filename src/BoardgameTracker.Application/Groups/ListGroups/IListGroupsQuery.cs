using BoardgameTracker.Core.GameSessions;

namespace BoardgameTracker.Application.Groups.ListGroups;

/// <summary>
/// Read boundary for retrieving the group-overview projection in the context of one selected game.
/// </summary>
public interface IListGroupsQuery
{
    Task<IReadOnlyList<GroupListItem>> ListGroupsAsync(
        string gameKey,
        bool includeTemporary = false,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);
}

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

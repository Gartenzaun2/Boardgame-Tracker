namespace BoardgameTracker.Application.Groups.ListGroups;

/// <summary>
/// Screen-oriented Application read model for one group in the group overview.
/// </summary>
public sealed record GroupListItem(
    Guid Id,
    string Name,
    bool IsTemporary,
    bool IsArchived,
    int MemberCount,
    IReadOnlyList<string> MemberNames,
    DateTimeOffset? LastPlayedAt);

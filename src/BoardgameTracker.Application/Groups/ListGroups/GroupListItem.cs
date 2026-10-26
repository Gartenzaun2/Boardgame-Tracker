namespace BoardgameTracker.Application.Groups.ListGroups;

public sealed record GroupListItem(
    Guid Id,
    string Name,
    bool IsTemporary,
    bool IsArchived,
    int MemberCount,
    IReadOnlyList<string> MemberNames,
    DateTimeOffset? LastPlayedAt);

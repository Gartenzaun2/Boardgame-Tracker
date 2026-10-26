namespace BoardgameTracker.Application.Groups.ListGroups;

public sealed record GroupListItem(
    Guid Id,
    string Name,
    int MemberCount,
    IReadOnlyList<string> MemberNames,
    DateTimeOffset? LastPlayedAt);

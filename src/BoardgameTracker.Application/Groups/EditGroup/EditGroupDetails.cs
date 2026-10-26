namespace BoardgameTracker.Application.Groups.EditGroup;

public sealed record EditGroupDetails(
    Guid Id,
    string Name,
    bool IsTemporary,
    IReadOnlyList<EditGroupMember> Members);

public sealed record EditGroupMember(
    Guid PersonId,
    string Name,
    bool IsGuest,
    int DefaultOrder);

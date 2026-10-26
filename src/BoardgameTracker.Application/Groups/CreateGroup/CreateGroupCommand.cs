namespace BoardgameTracker.Application.Groups.CreateGroup;

public sealed record CreateGroupCommand(
    string Name,
    bool IsTemporary,
    IReadOnlyList<CreateGroupMemberInput> Members);

public sealed record CreateGroupMemberInput(
    Guid? ExistingPersonId,
    string Name,
    bool IsGuest);

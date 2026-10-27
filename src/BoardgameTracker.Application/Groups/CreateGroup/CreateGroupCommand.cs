namespace BoardgameTracker.Application.Groups.CreateGroup;

/// <summary>
/// Input for the create-group use case, including all members that must be persisted atomically with the group.
/// </summary>
public sealed record CreateGroupCommand(
    string Name,
    bool IsTemporary,
    IReadOnlyList<CreateGroupMemberInput> Members);

/// <summary>
/// Describes one existing or newly entered member in a create-group request.
/// </summary>
public sealed record CreateGroupMemberInput(
    Guid? ExistingPersonId,
    string Name,
    bool IsGuest);

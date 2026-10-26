namespace BoardgameTracker.Application.Groups.EditGroup;

public sealed record EditGroupCommand(
    Guid GroupId,
    string Name,
    bool IsTemporary,
    IReadOnlyList<Guid> OrderedPersonIds);

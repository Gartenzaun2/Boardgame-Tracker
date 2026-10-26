namespace BoardgameTracker.Application.Groups.EditGroup;

public sealed record EditGroupCommand(
    Guid GroupId,
    string Name,
    bool MakePermanent,
    bool IsArchived,
    IReadOnlyList<Guid> OrderedPersonIds);

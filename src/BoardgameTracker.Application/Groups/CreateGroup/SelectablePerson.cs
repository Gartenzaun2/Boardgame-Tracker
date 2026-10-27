namespace BoardgameTracker.Application.Groups.CreateGroup;

/// <summary>
/// Read model for an existing active person that may be selected while creating a group.
/// </summary>
public sealed record SelectablePerson(
    Guid Id,
    string Name,
    bool IsGuest);

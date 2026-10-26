namespace BoardgameTracker.Application.Groups.CreateGroup;

public sealed record SelectablePerson(
    Guid Id,
    string Name,
    bool IsGuest);

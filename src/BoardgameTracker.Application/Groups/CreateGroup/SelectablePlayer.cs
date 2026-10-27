namespace BoardgameTracker.Application.Groups.CreateGroup;

/// <summary>
/// Read model for an existing active player that may be selected while creating a group.
/// </summary>
public sealed record SelectablePlayer(
    Guid Id,
    string Name,
    bool IsGuest);

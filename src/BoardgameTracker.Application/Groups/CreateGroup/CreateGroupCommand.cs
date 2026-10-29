using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Groups.CreateGroup;

/// <summary>
/// Input for the create-group use case, including all players that must be persisted atomically with the group.
/// </summary>
public sealed record CreateGroupCommand(
    string Name,
    bool IsTemporary,
    IReadOnlyList<CreateGroupPlayerInput> Players,
    string? GameKey = null);

/// <summary>
/// Describes one existing or newly entered player in a create-group request.
/// </summary>
public sealed record CreateGroupPlayerInput(
    Guid? ExistingPlayerId,
    string Name,
    bool IsGuest,
    PlayerColor? DefaultColor = null);

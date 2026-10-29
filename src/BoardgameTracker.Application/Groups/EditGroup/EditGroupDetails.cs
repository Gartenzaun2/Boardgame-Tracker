using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Groups.EditGroup;

/// <summary>
/// Application read model containing exactly the persisted group state needed by the edit screen.
/// </summary>
public sealed record EditGroupDetails(
    Guid Id,
    string Name,
    bool IsTemporary,
    bool IsArchived,
    IReadOnlyList<EditGroupPlayer> Players);

/// <summary>
/// Projected player data used while editing a group.
/// </summary>
public sealed record EditGroupPlayer(
    Guid PlayerId,
    string Name,
    bool IsGuest,
    int DefaultOrder,
    PlayerColor? DefaultColor = null);

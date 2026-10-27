using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Groups.EditGroup;

/// <summary>
/// Application read model containing exactly the persisted group state needed by the edit screen.
/// </summary>
public sealed record EditGroupDetails(
    Guid Id,
    string Name,
    bool IsTemporary,
    bool IsArchived,
    IReadOnlyList<EditGroupMember> Members);

/// <summary>
/// Projected member data used while editing a group, including its Ark Nova default color.
/// </summary>
public sealed record EditGroupMember(
    Guid PersonId,
    string Name,
    bool IsGuest,
    int DefaultOrder,
    ArkNovaPlayerColor? ArkNovaDefaultColor);

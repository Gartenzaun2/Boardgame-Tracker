using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Groups.EditGroup;

public sealed record EditGroupDetails(
    Guid Id,
    string Name,
    bool IsTemporary,
    bool IsArchived,
    IReadOnlyList<EditGroupMember> Members);

public sealed record EditGroupMember(
    Guid PersonId,
    string Name,
    bool IsGuest,
    int DefaultOrder,
    ArkNovaPlayerColor? ArkNovaDefaultColor);

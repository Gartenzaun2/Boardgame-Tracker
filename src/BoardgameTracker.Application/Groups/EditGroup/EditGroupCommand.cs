using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Groups.EditGroup;

/// <summary>
/// Input for editing mutable group metadata, default order, lifecycle flags and Ark Nova default colors without changing membership.
/// </summary>
public sealed record EditGroupCommand(
    Guid GroupId,
    string Name,
    bool MakePermanent,
    bool IsArchived,
    IReadOnlyList<Guid> OrderedPersonIds,
    IReadOnlyDictionary<Guid, ArkNovaPlayerColor?> ArkNovaDefaultColors);

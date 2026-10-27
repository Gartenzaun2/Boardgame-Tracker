using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.Players;

namespace BoardgameTracker.Application.Groups.EditGroup;

/// <summary>
/// Input for editing mutable group metadata, default order, lifecycle flags and Ark Nova default colors without changing membership.
/// </summary>
public sealed record EditGroupCommand(
    Guid GroupId,
    string Name,
    bool MakePermanent,
    bool IsArchived,
    IReadOnlyList<Guid> OrderedPlayerIds,
    IReadOnlyDictionary<Guid, ArkNovaPlayerColor?> ArkNovaDefaultColors);

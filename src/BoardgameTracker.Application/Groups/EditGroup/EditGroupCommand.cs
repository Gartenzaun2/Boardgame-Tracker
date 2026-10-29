using BoardgameTracker.Application.Groups.Colors;

namespace BoardgameTracker.Application.Groups.EditGroup;

/// <summary>Game-independent changes to fixed-membership group metadata and order.</summary>
public sealed record EditGroupCommand(
    Guid GroupId,
    string Name,
    bool MakePermanent,
    bool IsArchived,
    IReadOnlyList<Guid> OrderedPlayerIds,
    GroupColorDefaults? ColorDefaults = null);

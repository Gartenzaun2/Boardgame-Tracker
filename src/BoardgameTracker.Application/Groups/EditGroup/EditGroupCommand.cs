using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Groups.EditGroup;

public sealed record EditGroupCommand(
    Guid GroupId,
    string Name,
    bool MakePermanent,
    bool IsArchived,
    IReadOnlyList<Guid> OrderedPersonIds,
    IReadOnlyDictionary<Guid, ArkNovaPlayerColor?> ArkNovaDefaultColors);

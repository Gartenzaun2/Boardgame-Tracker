using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.Players;

namespace BoardgameTracker.Application.Groups.EditGroup;

/// <summary>
/// Application-owned persistence boundary for the group-edit use case.
/// </summary>
public interface IEditGroupDataAccess
{
    Task<EditGroupDetails?> GetDetailsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    Task<Group?> GetAggregateAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Group group,
        IReadOnlyDictionary<Guid, ArkNovaPlayerColor?> arkNovaDefaultColors,
        CancellationToken cancellationToken = default);
}

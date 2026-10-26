using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Groups.EditGroup;

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

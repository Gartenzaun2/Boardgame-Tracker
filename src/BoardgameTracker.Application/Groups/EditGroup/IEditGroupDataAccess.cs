using BoardgameTracker.Core.Groups;

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
        CancellationToken cancellationToken = default);
}

using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Application.Groups.EditGroup;

/// <summary>
/// Application-owned persistence boundary for the group-edit use case.
/// </summary>
public interface IEditGroupStore
{
    Task<EditGroupDetails?> GetDetailsAsync(
        Guid groupId,
        string? gameKey,
        CancellationToken cancellationToken = default);

    Task<Group?> GetAggregateAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Group group,
        GroupColorDefaults? colors,
        CancellationToken cancellationToken = default);
}

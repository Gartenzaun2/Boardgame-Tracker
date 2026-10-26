namespace BoardgameTracker.Application.Groups.ListGroups;

public interface IListGroupsQuery
{
    Task<IReadOnlyList<GroupListItem>> ExecuteAsync(
        bool includeTemporary = false,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);
}

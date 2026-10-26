namespace BoardgameTracker.Application.Groups.ListGroups;

public interface IListGroupsQuery
{
    Task<IReadOnlyList<GroupListItem>> ExecuteAsync(
        CancellationToken cancellationToken = default);
}

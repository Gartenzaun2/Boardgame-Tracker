namespace BoardgameTracker.Application.Groups.ListGroups;

/// <summary>
/// Read boundary for retrieving the group-overview projection without exposing the concrete SQLite query to the App project.
/// </summary>
public interface IListGroupsQuery
{
    Task<IReadOnlyList<GroupListItem>> ExecuteAsync(
        bool includeTemporary = false,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);
}

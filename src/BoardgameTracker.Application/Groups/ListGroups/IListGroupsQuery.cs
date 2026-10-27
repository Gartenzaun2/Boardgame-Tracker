namespace BoardgameTracker.Application.Groups.ListGroups;

/// <summary>
/// Read boundary for retrieving the group-overview projection in the context of one selected game.
/// </summary>
public interface IListGroupsQuery
{
    Task<IReadOnlyList<GroupListItem>> ExecuteAsync(
        string gameKey,
        bool includeTemporary = false,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);
}

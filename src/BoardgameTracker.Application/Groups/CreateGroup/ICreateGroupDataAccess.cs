using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.People;

namespace BoardgameTracker.Application.Groups.CreateGroup;

/// <summary>
/// Application-owned persistence boundary containing only the technical operations required by the create-group use case.
/// </summary>
public interface ICreateGroupDataAccess
{
    Task<IReadOnlyList<SelectablePerson>> GetSelectablePeopleAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> GetActivePersonIdsAsync(
        IReadOnlyCollection<Guid> personIds,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Group group,
        IReadOnlyCollection<Person> newPeople,
        CancellationToken cancellationToken = default);
}

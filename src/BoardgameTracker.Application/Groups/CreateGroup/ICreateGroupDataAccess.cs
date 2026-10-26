using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.People;

namespace BoardgameTracker.Application.Groups.CreateGroup;

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

using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.People;

namespace BoardgameTracker.Application.Groups.CreateGroup;

/// <summary>
/// Creates a valid fixed-membership group and coordinates person reuse/creation through the slice-specific persistence port.
/// </summary>
public sealed class CreateGroupUseCase(ICreateGroupDataAccess dataAccess)
{
    public Task<IReadOnlyList<SelectablePerson>> GetSelectablePeopleAsync(
        CancellationToken cancellationToken = default) =>
        dataAccess.GetSelectablePeopleAsync(cancellationToken);

    public async Task<Guid> ExecuteAsync(
        CreateGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        ArgumentNullException.ThrowIfNull(command.Members);

        if (command.Members.Count == 0)
        {
            throw new ArgumentException(
                "A group must contain at least one person.",
                nameof(command));
        }

        IReadOnlyList<Guid> requestedExistingIds =
        [
            .. command.Members
                .Where(member => member.ExistingPersonId.HasValue)
                .Select(member => member.ExistingPersonId.GetValueOrDefault())
        ];

        if (requestedExistingIds.Distinct().Count() != requestedExistingIds.Count)
        {
            throw new ArgumentException(
                "An existing person can only be selected once.",
                nameof(command));
        }

        IReadOnlySet<Guid> activePersonIds =
            await dataAccess.GetActivePersonIdsAsync(
                requestedExistingIds,
                cancellationToken);

        if (activePersonIds.Count != requestedExistingIds.Count)
        {
            throw new InvalidOperationException(
                "At least one selected person does not exist or is inactive.");
        }

        List<Person> newPeople = [];
        List<Guid> memberIds = [];

        foreach (CreateGroupMemberInput member in command.Members)
        {
            if (member.ExistingPersonId is Guid existingPersonId)
            {
                memberIds.Add(existingPersonId);
                continue;
            }

            Person person = new(member.Name, member.IsGuest);
            newPeople.Add(person);
            memberIds.Add(person.Id);
        }

        Group group = new(command.Name, command.IsTemporary, memberIds);

        await dataAccess.SaveAsync(group, newPeople, cancellationToken);
        return group.Id;
    }
}

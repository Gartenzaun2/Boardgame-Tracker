using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Groups.EditGroup;

/// <summary>
/// Loads and updates the mutable parts of an existing fixed-membership group through a slice-specific persistence boundary.
/// </summary>
public sealed class EditGroupUseCase(IEditGroupDataAccess dataAccess)
{
    public Task<EditGroupDetails?> GetAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        return dataAccess.GetDetailsAsync(groupId, cancellationToken);
    }

    public async Task ExecuteAsync(
        EditGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfEqual(command.GroupId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        ArgumentNullException.ThrowIfNull(command.OrderedPersonIds);
        ArgumentNullException.ThrowIfNull(command.ArkNovaDefaultColors);

        Group group = await dataAccess.GetAggregateAsync(
            command.GroupId,
            cancellationToken)
            ?? throw new InvalidOperationException("The group does not exist.");

        HashSet<Guid> memberIds = [.. group.Members.Select(member => member.PersonId)];

        if (!memberIds.SetEquals(command.ArkNovaDefaultColors.Keys))
        {
            throw new ArgumentException(
                "Ark Nova default colors must describe exactly the fixed group members.",
                nameof(command));
        }

        ArkNovaPlayerColor[] assignedColors =
        [
            .. command.ArkNovaDefaultColors.Values
                .Where(color => color.HasValue)
                .Select(color => color.GetValueOrDefault())
        ];

        if (assignedColors.Any(color => !Enum.IsDefined(color)))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "Ark Nova default colors must use defined player colors.");
        }

        if (assignedColors.Distinct().Count() != assignedColors.Length)
        {
            throw new InvalidOperationException(
                "Ark Nova default colors must be unique within a group.");
        }

        group.SetName(command.Name);

        if (command.MakePermanent)
        {
            group.MakePermanent();
        }

        group.SetArchived(command.IsArchived);
        group.SetDefaultOrder(command.OrderedPersonIds);

        await dataAccess.SaveAsync(group, command.ArkNovaDefaultColors, cancellationToken);
    }
}

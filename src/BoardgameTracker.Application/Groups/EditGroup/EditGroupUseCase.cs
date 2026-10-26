using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Application.Groups.EditGroup;

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

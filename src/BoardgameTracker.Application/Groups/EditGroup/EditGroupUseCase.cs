using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Core.Groups;

namespace BoardgameTracker.Application.Groups.EditGroup;

/// <summary>
/// Loads and updates the mutable parts of an existing fixed-membership group through a slice-specific persistence boundary.
/// </summary>
public sealed class EditGroupUseCase(IEditGroupDataAccess dataAccess, GamePlayerColorCatalog colorCatalog)
{
    public Task<EditGroupDetails?> GetAsync(
        Guid groupId,
        string? gameKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        return dataAccess.GetDetailsAsync(groupId, gameKey, cancellationToken);
    }

    public async Task ExecuteAsync(
        EditGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        Group group = await PrepareAsync(command, cancellationToken);
        await dataAccess.SaveAsync(group, command.ColorDefaults, cancellationToken);
    }

    private async Task<Group> PrepareAsync(EditGroupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfEqual(command.GroupId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        ArgumentNullException.ThrowIfNull(command.OrderedPlayerIds);

        Group group = await dataAccess.GetAggregateAsync(command.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("The group does not exist.");

        if (command.ColorDefaults is { } defaults)
        {
            if (!group.Players.Select(player => player.PlayerId).ToHashSet().SetEquals(defaults.Colors.Keys))
                throw new ArgumentException("Colors must describe exactly the fixed group players.", nameof(command));
            colorCatalog.Get(defaults.GameKey).Validate(defaults.Colors.Values);
        }

        group.SetName(command.Name);

        if (command.MakePermanent)
        {
            group.MakePermanent();
        }

        group.SetArchived(command.IsArchived);
        group.SetDefaultOrder(command.OrderedPlayerIds);

        return group;
    }
}

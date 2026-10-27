using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Groups.CreateGroup;

/// <summary>
/// Creates a valid fixed-membership group and coordinates player reuse/creation through the slice-specific persistence port.
/// </summary>
public sealed class CreateGroupUseCase(ICreateGroupDataAccess dataAccess)
{
    public Task<IReadOnlyList<SelectablePlayer>> GetSelectablePlayersAsync(
        CancellationToken cancellationToken = default) =>
        dataAccess.GetSelectablePlayersAsync(cancellationToken);

    public async Task<Guid> ExecuteAsync(
        CreateGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        ArgumentNullException.ThrowIfNull(command.Players);

        if (command.Players.Count == 0)
        {
            throw new ArgumentException(
                "A group must contain at least one player.",
                nameof(command));
        }

        IReadOnlyList<Guid> requestedExistingIds =
        [
            .. command.Players
                .Where(player => player.ExistingPlayerId.HasValue)
                .Select(player => player.ExistingPlayerId.GetValueOrDefault())
        ];

        if (requestedExistingIds.Distinct().Count() != requestedExistingIds.Count)
        {
            throw new ArgumentException(
                "An existing player can only be selected once.",
                nameof(command));
        }

        IReadOnlySet<Guid> activePlayerIds =
            await dataAccess.GetActivePlayerIdsAsync(
                requestedExistingIds,
                cancellationToken);

        if (activePlayerIds.Count != requestedExistingIds.Count)
        {
            throw new InvalidOperationException(
                "At least one selected player does not exist or is inactive.");
        }

        List<Player> newPlayers = [];
        List<Guid> playerIds = [];

        foreach (CreateGroupPlayerInput player in command.Players)
        {
            if (player.ExistingPlayerId is Guid existingPlayerId)
            {
                playerIds.Add(existingPlayerId);
                continue;
            }

            Player newPlayer = new(player.Name, player.IsGuest);
            newPlayers.Add(newPlayer);
            playerIds.Add(newPlayer.Id);
        }

        Group group = new(command.Name, command.IsTemporary, playerIds);

        await dataAccess.SaveAsync(group, newPlayers, cancellationToken);
        return group.Id;
    }
}

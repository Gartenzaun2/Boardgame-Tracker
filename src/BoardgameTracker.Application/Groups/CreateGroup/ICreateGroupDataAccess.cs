using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Groups.CreateGroup;

/// <summary>
/// Application-owned persistence boundary containing only the technical operations required by the create-group use case.
/// </summary>
public interface ICreateGroupDataAccess
{
    Task<IReadOnlyList<SelectablePlayer>> GetSelectablePlayersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> GetActivePlayerIdsAsync(
        IReadOnlyCollection<Guid> playerIds,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Group group,
        IReadOnlyCollection<Player> newPlayers,
        CancellationToken cancellationToken = default);
}

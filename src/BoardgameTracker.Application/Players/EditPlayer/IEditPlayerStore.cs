using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Players.EditPlayer;

public interface IEditPlayerStore
{
    Task<Player?> GetAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task SaveAsync(Player player, CancellationToken cancellationToken = default);
}

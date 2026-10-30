using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Players.EditPlayer;

public sealed class EditPlayerUseCase(IEditPlayerStore store)
{
    public Task<Player?> GetAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        return store.GetAsync(playerId, cancellationToken);
    }

    public async Task UpdatePlayerAsync(Guid playerId, string name, bool isArchived, CancellationToken cancellationToken = default)
    {
        Player player = await GetAsync(playerId, cancellationToken)
            ?? throw new InvalidOperationException("The player does not exist.");
        player.SetName(name);
        player.SetArchived(isArchived);
        await store.SaveAsync(player, cancellationToken);
    }
}

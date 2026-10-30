using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Players.EditPlayer;

public interface IEditPlayerDataAccess
{
    Task<Player?> GetAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task SaveAsync(Player player, CancellationToken cancellationToken = default);
}

public sealed class EditPlayerUseCase(IEditPlayerDataAccess dataAccess)
{
    public Task<Player?> GetAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(playerId, Guid.Empty);
        return dataAccess.GetAsync(playerId, cancellationToken);
    }

    public async Task ExecuteAsync(Guid playerId, string name, bool isArchived, CancellationToken cancellationToken = default)
    {
        Player player = await GetAsync(playerId, cancellationToken)
            ?? throw new InvalidOperationException("The player does not exist.");
        player.SetName(name);
        player.SetArchived(isArchived);
        await dataAccess.SaveAsync(player, cancellationToken);
    }
}

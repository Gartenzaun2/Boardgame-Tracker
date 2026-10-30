using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.Groups;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Infrastructure.Persistence.SQLite;
using BoardgameTracker.Infrastructure.Persistence.SQLite.Groups;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Infrastructure.Games.ArkNova.Persistence.SQLite.Groups;

public sealed class ArkNovaGroupColorPersistence : IGroupColorPersistence
{
    public string GameKey => ArkNovaGame.Key;
    public async Task<IReadOnlyDictionary<Guid, PlayerColor?>> ReadAsync(BoardgameTrackerDbContext dbContext, Guid groupId, CancellationToken cancellationToken)
        => await dbContext.ArkNovaGroupPlayers.AsNoTracking().Where(player => player.GroupId == groupId)
            .ToDictionaryAsync(player => player.PlayerId, player => (PlayerColor?)player.DefaultColor, cancellationToken);

    public async Task ReplaceAsync(BoardgameTrackerDbContext dbContext, Guid groupId, IReadOnlyDictionary<Guid, PlayerColor?> colors, CancellationToken cancellationToken)
    {
        ArkNovaPlayerColors.Rules.Validate(colors.Values);
        await dbContext.ArkNovaGroupPlayers.Where(player => player.GroupId == groupId).ExecuteDeleteAsync(cancellationToken);
        dbContext.ArkNovaGroupPlayers.AddRange(colors.Where(pair => pair.Value.HasValue)
            .Select(pair => new ArkNovaGroupPlayer(groupId, pair.Key, pair.Value!.Value)));
    }
}

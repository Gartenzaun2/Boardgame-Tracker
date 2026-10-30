using BoardgameTracker.Core.Games.ArkNova;
using BoardgameTracker.Core.Games.ArkNova.Groups;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Application.Groups;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ArkNova.Persistence;

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

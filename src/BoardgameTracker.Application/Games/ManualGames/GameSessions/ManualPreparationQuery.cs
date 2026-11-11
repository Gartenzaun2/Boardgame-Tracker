using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Games.ManualGames;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Games.ManualGames.GameSessions;

public sealed class ManualPreparationQuery(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task<ManualPreparationData> GetAsync(string gameKey, Guid groupId, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        ManualPreparationContext context = await LoadContextAsync(db, gameKey, groupId, cancellationToken);
        var players = await (from membership in db.GroupPlayers.AsNoTracking()
                             join player in db.Players.AsNoTracking() on membership.PlayerId equals player.Id
                             join color in db.GroupGamePlayerColors.AsNoTracking().Where(color => color.GameId == context.Game.Id)
                                 on new { membership.GroupId, membership.PlayerId } equals new { color.GroupId, color.PlayerId } into colors
                             from color in colors.DefaultIfEmpty()
                             where membership.GroupId == groupId
                             orderby membership.DefaultOrder
                             select new ManualPreparationPlayer(player.Id, player.Name, membership.DefaultOrder,
                                 color == null ? (PlayerColor?)null : color.DefaultColor)).ToArrayAsync(cancellationToken);
        var completed = await db.GameSessions.AsNoTracking()
            .Where(session => session.GameId == context.Game.Id && session.GroupId == groupId && session.Status == GameSessionStatus.Completed)
            .Select(session => new { session.Id, session.StartedAt }).ToArrayAsync(cancellationToken);
        var last = completed.OrderByDescending(session => session.StartedAt).ThenBy(session => session.Id).FirstOrDefault();
        ManualLastSession? lastSession = null;
        if (last is not null)
        {
            ManualLastPlayer[] results = await (from participant in db.GameSessionPlayers.AsNoTracking()
                join player in db.Players.AsNoTracking() on participant.PlayerId equals player.Id
                join detail in db.ManualGameSessionPlayers.AsNoTracking()
                    on new { participant.GameSessionId, participant.PlayerId } equals new { detail.GameSessionId, detail.PlayerId } into details
                from detail in details.DefaultIfEmpty()
                where participant.GameSessionId == last.Id
                select new ManualLastPlayer(participant.PlayerId, player.Name, participant.Placement,
                    detail == null ? (int?)null : detail.Points)).ToArrayAsync(cancellationToken);
            lastSession = new(last.StartedAt, results);
        }
        return new(context.Game.Id, context.Game.Key, context.Game.Name, context.Game.BadgeText, context.Group.Id,
            context.Group.Name, context.Configuration.CapturePlayerOrder, context.Configuration.CapturePlayerColors, players, lastSession);
    }

    // Both preparation and start validate against the complete, current persisted membership.
    internal static async Task<ManualPreparationContext> LoadContextAsync(BoardgameTrackerDbContext db, string gameKey,
        Guid groupId, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        if (!ManualGame.MatchesKey(gameKey))
            throw new InvalidOperationException("The selected game does not belong to ManualGames.");
        Game game = await db.Games.AsNoTracking().SingleOrDefaultAsync(game => game.Key == gameKey, token)
            ?? throw new InvalidOperationException("The selected game does not exist.");
        ManualGame config = await db.ManualGames.AsNoTracking().SingleOrDefaultAsync(manual => manual.GameId == game.Id, token)
            ?? throw new InvalidOperationException("Manual game configuration is missing.");
        Group group = await db.Groups.AsNoTracking().Include(group => group.Players).SingleOrDefaultAsync(group => group.Id == groupId, token)
            ?? throw new InvalidOperationException("The selected group does not exist.");
        if (group.IsArchived)
            throw new InvalidOperationException("An archived group cannot start a session.");
        if (!await db.GroupGames.AnyAsync(assignment => assignment.GameId == game.Id && assignment.GroupId == groupId, token))
            throw new InvalidOperationException("The group is not assigned to the selected game.");
        if (!game.SupportsPlayerCount(group.Players.Count))
            throw new InvalidOperationException("The complete group membership is outside the game's player limits.");
        if (await db.GameSessions.AnyAsync(session => session.GameId == game.Id && session.GroupId == groupId
            && (session.Status == GameSessionStatus.Running || session.Status == GameSessionStatus.Scoring), token))
            throw new InvalidOperationException("This game and group already have an open session.");
        return new(game, config, group);
    }
}

internal sealed record ManualPreparationContext(Game Game, ManualGame Configuration, Group Group);
public sealed record ManualPreparationData(Guid GameId, string GameKey, string GameName, string BadgeText, Guid GroupId,
    string GroupName, bool CapturePlayerOrder, bool CapturePlayerColors, IReadOnlyList<ManualPreparationPlayer> Players, ManualLastSession? LastSession);
public sealed record ManualPreparationPlayer(Guid PlayerId, string Name, int DefaultOrder, PlayerColor? DefaultColor);
public sealed record ManualLastSession(DateTimeOffset StartedAt, IReadOnlyList<ManualLastPlayer> Players);
public sealed record ManualLastPlayer(Guid PlayerId, string Name, int? Placement, int? Points);

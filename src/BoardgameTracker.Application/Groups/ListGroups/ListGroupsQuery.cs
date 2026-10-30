using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.Application.Groups.ListGroups;

/// <summary>
/// EF Core implementation of the game-context group-overview read projection.
/// Groups remain game-independent; gameSession metadata is filtered to the selected game.
/// </summary>
public sealed class ListGroupsQuery(
    IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task<IReadOnlyList<GroupListItem>> ListGroupsAsync(
        string gameKey,
        bool includeTemporary = false,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var groups = await dbContext.Groups
            .AsNoTracking()
            .Where(group => includeTemporary || !group.IsTemporary)
            .Where(group => includeArchived || !group.IsArchived)
            .Select(group => new { group.Id, group.Name, group.IsTemporary, group.IsArchived })
            .ToListAsync(cancellationToken);

        Guid[] groupIds = [.. groups.Select(group => group.Id)];
        if (groupIds.Length == 0)
        {
            return [];
        }

        Guid? gameId = await dbContext.Games
            .AsNoTracking()
            .Where(game => game.Key == gameKey)
            .Select(game => (Guid?)game.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var players = await (
            from membership in dbContext.GroupPlayers.AsNoTracking()
            where groupIds.Contains(membership.GroupId)
            join player in dbContext.Players.AsNoTracking()
                on membership.PlayerId equals player.Id
            select new
            {
                membership.GroupId,
                membership.DefaultOrder,
                player.Name
            })
            .ToListAsync(cancellationToken);

        List<GroupGameSessionDateProjection> gameSessionDates = gameId is null
            ? []
            : await dbContext.GameSessions
                .AsNoTracking()
                .Where(gameSession => groupIds.Contains(gameSession.GroupId)
                                  && gameSession.GameId == gameId.Value)
                .Select(gameSession => new GroupGameSessionDateProjection(gameSession.GroupId, gameSession.StartedAt))
                .ToListAsync(cancellationToken);

        Dictionary<Guid, DateTimeOffset?> lastPlayedByGroup = gameSessionDates
            .GroupBy(gameSession => gameSession.GroupId)
            .ToDictionary(
                group => group.Key,
                group => (DateTimeOffset?)group.Max(gameSession => gameSession.StartedAt));

        List<GroupOpenGameSessionProjection> openGameSessions = gameId is null
            ? []
            : await dbContext.GameSessions
                .AsNoTracking()
                .Where(gameSession => groupIds.Contains(gameSession.GroupId)
                                  && gameSession.GameId == gameId.Value)
                .Where(gameSession => gameSession.Status == GameSessionStatus.Running
                               || gameSession.Status == GameSessionStatus.Scoring)
                .Select(gameSession => new GroupOpenGameSessionProjection(
                    gameSession.GroupId,
                    gameSession.Id,
                    gameSession.Status,
                    gameSession.ModifiedAt))
                .ToListAsync(cancellationToken);

        var openGameSessionByGroup = openGameSessions
            .GroupBy(gameSession => gameSession.GroupId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(gameSession => gameSession.ModifiedAt).First());

        Dictionary<Guid, string[]> playerNamesByGroup = players
            .GroupBy(player => player.GroupId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(player => player.DefaultOrder)
                    .Select(player => player.Name)
                    .ToArray());

        return
        [
            .. groups
                .Select(group =>
                {
                    string[] playerNames = playerNamesByGroup.GetValueOrDefault(group.Id) ?? [];

                    return new GroupListItem(
                        group.Id,
                        group.Name,
                        group.IsTemporary,
                        group.IsArchived,
                        playerNames.Length,
                        playerNames,
                        lastPlayedByGroup.GetValueOrDefault(group.Id),
                        openGameSessionByGroup.TryGetValue(group.Id, out var openGameSession)
                            ? openGameSession.Id
                            : null,
                        openGameSessionByGroup.TryGetValue(group.Id, out openGameSession)
                            ? openGameSession.Status
                            : null);
                })
                .OrderByDescending(group => group.LastPlayedAt)
                .ThenBy(group => group.Name)
        ];
    }

    private sealed record GroupGameSessionDateProjection(Guid GroupId, DateTimeOffset StartedAt);

    private sealed record GroupOpenGameSessionProjection(
        Guid GroupId,
        Guid Id,
        GameSessionStatus Status,
        DateTimeOffset ModifiedAt);
}

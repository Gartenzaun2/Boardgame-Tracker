using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Groups;

/// <summary>Changes group metadata, fixed-member order and current game assignments atomically.</summary>
public sealed class EditGroupUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task UpdateGroupAsync(EditGroupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfEqual(command.GroupId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        ArgumentNullException.ThrowIfNull(command.OrderedPlayerIds);
        ArgumentNullException.ThrowIfNull(command.GameIds);
        Guid[] gameIds = command.GameIds.Distinct().ToArray();
        if (gameIds.Contains(Guid.Empty))
            throw new ArgumentException("Game IDs must not be empty.", nameof(command));

        await using BoardgameTrackerDbContext dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        // Direct order updates use a temporary range to avoid unique-index collisions.
        // Keep this aggregate detached so SaveChanges cannot replay those order changes.
        Group group = await dbContext.Groups.AsNoTracking().Include(group => group.Players)
            .SingleOrDefaultAsync(group => group.Id == command.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("The group does not exist.");
        group.SetName(command.Name);
        group.SetArchived(command.IsArchived);
        group.SetDefaultOrder(command.OrderedPlayerIds);

        if (command.IsArchived && await dbContext.GameSessions.AnyAsync(session => session.GroupId == group.Id
            && (session.Status == GameSessionStatus.Running || session.Status == GameSessionStatus.Scoring), cancellationToken))
            throw new InvalidOperationException("A group with an open game session cannot be archived.");
        if (await dbContext.Games.CountAsync(game => gameIds.Contains(game.Id), cancellationToken) != gameIds.Length)
            throw new InvalidOperationException("At least one selected game does not exist.");
        Guid[] assignedIds = await dbContext.GroupGames.Where(assignment => assignment.GroupId == group.Id)
            .Select(assignment => assignment.GameId).ToArrayAsync(cancellationToken);
        Guid[] removedIds = assignedIds.Except(gameIds).ToArray();
        if (await dbContext.GameSessions.AnyAsync(session => session.GroupId == group.Id && removedIds.Contains(session.GameId)
            && (session.Status == GameSessionStatus.Running || session.Status == GameSessionStatus.Scoring), cancellationToken))
            throw new InvalidOperationException("A game assignment with an open game session cannot be removed.");

        await SaveGroupChangesAsync(dbContext, group, cancellationToken);
        await dbContext.GroupGames.Where(assignment => assignment.GroupId == group.Id && removedIds.Contains(assignment.GameId))
            .ExecuteDeleteAsync(cancellationToken);
        dbContext.GroupGames.AddRange(gameIds.Except(assignedIds).Select(gameId => new GroupGame(group.Id, gameId)));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<EditGroupDetails?> GetAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        await using BoardgameTrackerDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var group = await db.Groups.AsNoTracking().Where(group => group.Id == groupId)
            .Select(group => new { group.Id, group.Name, group.IsArchived }).SingleOrDefaultAsync(cancellationToken);
        if (group is null)
            return null;
        EditGroupPlayer[] players = await (
            from membership in db.GroupPlayers.AsNoTracking()
            join player in db.Players.AsNoTracking() on membership.PlayerId equals player.Id
            where membership.GroupId == groupId
            orderby membership.DefaultOrder
            select new EditGroupPlayer(player.Id, player.Name, player.IsGuest, membership.DefaultOrder))
            .ToArrayAsync(cancellationToken);
        Guid[] assignedIds = await db.GroupGames.AsNoTracking().Where(assignment => assignment.GroupId == groupId)
            .Select(assignment => assignment.GameId).ToArrayAsync(cancellationToken);
        Guid[] openGameIds = await db.GameSessions.AsNoTracking().Where(session => session.GroupId == groupId
            && (session.Status == GameSessionStatus.Running || session.Status == GameSessionStatus.Scoring))
            .Select(session => session.GameId).Distinct().ToArrayAsync(cancellationToken);
        EditGroupGame[] games = await db.Games.AsNoTracking().OrderBy(game => game.Key)
            .Select(game => new EditGroupGame(game.Id, game.Key, assignedIds.Contains(game.Id), !openGameIds.Contains(game.Id)))
            .ToArrayAsync(cancellationToken);
        return new(group.Id, group.Name, group.IsArchived, players,
            games.Where(game => players.Length <= db.GetMaximumPlayers(game.Key)).ToArray(), openGameIds.Length == 0);
    }

    private static async Task SaveGroupChangesAsync(
        BoardgameTrackerDbContext dbContext, Group group, CancellationToken cancellationToken)
    {
        int updatedGroups = await dbContext.Groups
            .Where(candidate => candidate.Id == group.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(candidate => candidate.Name, group.Name)
                    .SetProperty(candidate => candidate.IsArchived, group.IsArchived),
                cancellationToken);

        if (updatedGroups != 1)
        {
            throw new InvalidOperationException("The group no longer exists.");
        }

        // Move all persisted orders outside the normal 1..n range first.
        // This avoids transient collisions with the unique (GroupId, DefaultOrder)
        // index while two players swap their positions.
        int orderOffset = group.Players.Count + 1000;

        await dbContext.GroupPlayers
            .Where(player => player.GroupId == group.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    player => player.DefaultOrder,
                    player => player.DefaultOrder + orderOffset),
                cancellationToken);

        foreach (GroupPlayer player in group.Players.OrderBy(player => player.DefaultOrder))
        {
            int updatedPlayers = await dbContext.GroupPlayers
                .Where(candidate =>
                    candidate.GroupId == group.Id
                    && candidate.PlayerId == player.PlayerId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        candidate => candidate.DefaultOrder,
                        player.DefaultOrder),
                    cancellationToken);

            if (updatedPlayers != 1)
            {
                throw new InvalidOperationException(
                    "A group player no longer exists.");
            }
        }

    }
}

public sealed record EditGroupCommand(Guid GroupId, string Name, bool IsArchived,
    IReadOnlyList<Guid> OrderedPlayerIds, IReadOnlyCollection<Guid> GameIds);
public sealed record EditGroupDetails(Guid Id, string Name, bool IsArchived,
    IReadOnlyList<EditGroupPlayer> Players, IReadOnlyList<EditGroupGame> Games, bool CanArchive);
public sealed record EditGroupPlayer(Guid PlayerId, string Name, bool IsGuest, int DefaultOrder);
public sealed record EditGroupGame(Guid Id, string Key, bool IsAssigned, bool CanRemove);

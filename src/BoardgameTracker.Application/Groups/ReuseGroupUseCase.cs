using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Groups;

public sealed class ReuseGroupUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task<Guid> ReuseGroupAsync(ReuseGroupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfEqual(command.GroupId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(command.ExpectedPlayerIds);
        ArgumentNullException.ThrowIfNull(command.GameIds);
        if (!Enum.IsDefined(command.Action))
            throw new ArgumentOutOfRangeException(nameof(command));
        Guid[] expectedPlayers = command.ExpectedPlayerIds.ToArray();
        if (expectedPlayers.Length == 0 || expectedPlayers.Contains(Guid.Empty) || expectedPlayers.Distinct().Count() != expectedPlayers.Length)
            throw new ArgumentException("Expected players must have distinct nonempty IDs.", nameof(command));
        Guid[] gameIds = command.GameIds.Distinct().ToArray();
        if (gameIds.Contains(Guid.Empty))
            throw new ArgumentException("Game IDs must not be empty.", nameof(command));

        await using BoardgameTrackerDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        Group group = await db.Groups.Include(group => group.Players).SingleOrDefaultAsync(group => group.Id == command.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("The selected group does not exist.");
        if (!group.Players.Select(player => player.PlayerId).ToHashSet().SetEquals(expectedPlayers))
            throw new InvalidOperationException("The selected group does not have the expected players.");
        if (group.IsArchived && command.Action != GroupReuseAction.Reactivate)
            throw new InvalidOperationException("An archived group requires explicit reactivation.");
        Game[] games = await db.Games.Where(game => gameIds.Contains(game.Id)).ToArrayAsync(cancellationToken);
        if (games.Length != gameIds.Length)
            throw new InvalidOperationException("At least one selected game does not exist.");

        if (command.Action == GroupReuseAction.Reactivate)
            group.SetArchived(false);
        Guid[] assignedIds = await db.GroupGames.Where(assignment => assignment.GroupId == group.Id)
            .Select(assignment => assignment.GameId).ToArrayAsync(cancellationToken);
        db.GroupGames.AddRange(games.Where(game => !assignedIds.Contains(game.Id))
            .Select(game => GroupGame.Create(group, game)));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return group.Id;
    }
}

/// <summary>Reuses one explicitly selected identity. Unsaved draft players and names are not persisted.</summary>
public sealed record ReuseGroupCommand(Guid GroupId, IReadOnlyList<Guid> ExpectedPlayerIds,
    IReadOnlyCollection<Guid> GameIds, GroupReuseAction Action);
public enum GroupReuseAction { UseExisting, Reactivate }

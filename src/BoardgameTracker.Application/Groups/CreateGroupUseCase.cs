using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Groups;

/// <summary>Explicitly creates a new group, including when another group has the same players.</summary>
public sealed class CreateGroupUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task<Guid> CreateGroupAsync(CreateGroupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        ArgumentNullException.ThrowIfNull(command.Players);
        ArgumentNullException.ThrowIfNull(command.GameIds);
        if (command.Players.Count == 0)
            throw new ArgumentException("A group must contain at least one player.", nameof(command));

        Guid[] existingPlayerIds = command.Players.Where(player => player.ExistingPlayerId.HasValue)
            .Select(player => player.ExistingPlayerId!.Value).ToArray();
        if (existingPlayerIds.Contains(Guid.Empty) || existingPlayerIds.Distinct().Count() != existingPlayerIds.Length)
            throw new ArgumentException("Existing players must have distinct nonempty IDs.", nameof(command));
        Guid[] gameIds = command.GameIds.Distinct().ToArray();
        if (gameIds.Contains(Guid.Empty))
            throw new ArgumentException("Game IDs must not be empty.", nameof(command));

        await using BoardgameTrackerDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        // Validation and all inserts share a transaction, including new players in the draft.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await db.Players.CountAsync(player => existingPlayerIds.Contains(player.Id) && !player.IsArchived, cancellationToken) != existingPlayerIds.Length)
            throw new InvalidOperationException("At least one selected player does not exist or is inactive.");
        if (await db.Games.CountAsync(game => gameIds.Contains(game.Id), cancellationToken) != gameIds.Length)
            throw new InvalidOperationException("At least one selected game does not exist.");

        List<Guid> playerIds = [];
        foreach (CreateGroupPlayerInput input in command.Players)
        {
            if (input.ExistingPlayerId is Guid existingId)
            {
                playerIds.Add(existingId);
                continue;
            }
            Player player = new(input.Name, input.IsGuest);
            db.Players.Add(player);
            playerIds.Add(player.Id);
        }
        Group group = new(command.Name, playerIds);
        db.Groups.Add(group);
        db.GroupGames.AddRange(gameIds.Select(gameId => new GroupGame(group.Id, gameId)));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return group.Id;
    }

    public async Task<IReadOnlyList<SelectablePlayer>> GetSelectablePlayersAsync(CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Players.AsNoTracking().Where(player => !player.IsArchived).OrderBy(player => player.Name)
            .Select(player => new SelectablePlayer(player.Id, player.Name, player.IsGuest)).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SelectableGroupGame>> GetSelectableGamesAsync(CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var games = await db.Games.AsNoTracking().OrderBy(game => game.Key)
            .Select(game => new { game.Id, game.Key }).ToArrayAsync(cancellationToken);
        return games.Select(game => new SelectableGroupGame(game.Id, game.Key, db.GetMaximumPlayers(game.Key))).ToArray();
    }
}

/// <summary>A deliberate new identity; duplicate membership does not trigger automatic reuse.</summary>
public sealed record CreateGroupCommand(string Name, IReadOnlyList<CreateGroupPlayerInput> Players, IReadOnlyCollection<Guid> GameIds);
public sealed record CreateGroupPlayerInput(Guid? ExistingPlayerId, string Name, bool IsGuest);
public sealed record SelectablePlayer(Guid Id, string Name, bool IsGuest);
public sealed record SelectableGroupGame(Guid Id, string Key, int MaximumPlayers);

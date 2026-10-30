using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Application.Persistence;

namespace BoardgameTracker.Application.Groups;

/// <summary>Creates the fixed-membership group, new players and game defaults atomically.</summary>
public sealed class CreateGroupUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory, GamePlayerColorCatalog colorCatalog, IEnumerable<IGroupColorPersistence> colorStores)
{
    public async Task<Guid> CreateGroupAsync(
        CreateGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        ArgumentNullException.ThrowIfNull(command.Players);

        if (command.Players.Count == 0)
        {
            throw new ArgumentException(
                "A group must contain at least one player.",
                nameof(command));
        }

        IReadOnlyList<Guid> requestedExistingIds =
        [
            .. command.Players
                .Where(player => player.ExistingPlayerId.HasValue)
                .Select(player => player.ExistingPlayerId.GetValueOrDefault())
        ];

        if (requestedExistingIds.Distinct().Count() != requestedExistingIds.Count)
        {
            throw new ArgumentException(
                "An existing player can only be selected once.",
                nameof(command));
        }

        // Each command owns its context; no tracked state survives another UI action.
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        IReadOnlySet<Guid> activePlayerIds =
            await GetActivePlayerIdsAsync(
                dbContext, requestedExistingIds,
                cancellationToken);

        if (activePlayerIds.Count != requestedExistingIds.Count)
        {
            throw new InvalidOperationException(
                "At least one selected player does not exist or is inactive.");
        }

        colorCatalog.Get(command.GameKey).Validate(command.Players.Select(player => player.DefaultColor));

        List<Player> newPlayers = [];
        List<Guid> playerIds = [];

        foreach (CreateGroupPlayerInput player in command.Players)
        {
            if (player.ExistingPlayerId is Guid existingPlayerId)
            {
                playerIds.Add(existingPlayerId);
                continue;
            }

            Player newPlayer = new(player.Name, player.IsGuest);
            newPlayers.Add(newPlayer);
            playerIds.Add(newPlayer.Id);
        }

        Group group = new(command.Name, command.IsTemporary, playerIds);

        GroupColorDefaults? colors = command.GameKey is null ? null : new(command.GameKey,
            playerIds.Select((id, index) => (id, command.Players[index].DefaultColor)).ToDictionary(p => p.id, p => p.DefaultColor));

        // Color modules can issue direct relational writes before SaveChanges.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (newPlayers.Count > 0)
        {
            dbContext.Players.AddRange(newPlayers);
        }

        // Group owns GroupPlayer. EF tracks the backing collection and inserts
        // the memberships together with the aggregate.
        dbContext.Groups.Add(group);

        if (colors is not null)
        {
            var store = colorStores.SingleOrDefault(store => store.GameKey == colors.GameKey);
            if (store is not null)
                await store.ReplaceAsync(dbContext, group.Id, colors.Colors, cancellationToken);
            else if (colors.Colors.Values.Any(color => color.HasValue))
                throw new InvalidOperationException("No color storage registered for this game.");
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return group.Id;
    }

    public async Task<IReadOnlyList<SelectablePlayer>> GetSelectablePlayersAsync(
        CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Players
            .AsNoTracking()
            .Where(player => !player.IsArchived)
            .OrderBy(player => player.Name)
            .Select(player => new SelectablePlayer(
                player.Id,
                player.Name,
                player.IsGuest))
            .ToListAsync(cancellationToken);
    }

    private static async Task<IReadOnlySet<Guid>> GetActivePlayerIdsAsync(
        BoardgameTrackerDbContext dbContext,
        IReadOnlyCollection<Guid> playerIds,
        CancellationToken cancellationToken = default)
    {
        if (playerIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        Guid[] requestedIds = [.. playerIds];

        Guid[] ids = await dbContext.Players
            .AsNoTracking()
            .Where(player => !player.IsArchived && requestedIds.Contains(player.Id))
            .Select(player => player.Id)
            .ToArrayAsync(cancellationToken);

        return ids.ToHashSet();
    }
}

/// <summary>
/// Input for the create-group use case, including all players that must be persisted atomically with the group.
/// </summary>
public sealed record CreateGroupCommand(
    string Name,
    bool IsTemporary,
    IReadOnlyList<CreateGroupPlayerInput> Players,
    string? GameKey = null);

/// <summary>
/// Describes one existing or newly entered player in a create-group request.
/// </summary>
public sealed record CreateGroupPlayerInput(
    Guid? ExistingPlayerId,
    string Name,
    bool IsGuest,
    PlayerColor? DefaultColor = null);

/// <summary>
/// Read model for an existing active player that may be selected while creating a group.
/// </summary>
public sealed record SelectablePlayer(
    Guid Id,
    string Name,
    bool IsGuest);

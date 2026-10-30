using BoardgameTracker.Application.Groups.Colors;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Core.Players;
using BoardgameTracker.Application.Persistence;

namespace BoardgameTracker.Application.Groups;

/// <summary>Changes group metadata, fixed-member order and module-owned defaults together.</summary>
public sealed class EditGroupUseCase(IDbContextFactory<BoardgameTrackerDbContext> contextFactory, GamePlayerColorCatalog colorCatalog, IEnumerable<IGroupColorPersistence> colorStores)
{
    public async Task UpdateGroupAsync(EditGroupCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfEqual(command.GroupId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Name);
        ArgumentNullException.ThrowIfNull(command.OrderedPlayerIds);

        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        // Direct order updates use a temporary range to avoid unique-index collisions.
        // Keep this aggregate detached so SaveChanges cannot replay those order changes.
        Group group = await dbContext.Groups.AsNoTracking().Include(group => group.Players)
            .SingleOrDefaultAsync(group => group.Id == command.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("The group does not exist.");

        if (command.ColorDefaults is { } defaults)
        {
            if (!group.Players.Select(player => player.PlayerId).ToHashSet().SetEquals(defaults.Colors.Keys))
                throw new ArgumentException("Colors must describe exactly the fixed group players.", nameof(command));
            colorCatalog.Get(defaults.GameKey).Validate(defaults.Colors.Values);
        }

        group.SetName(command.Name);

        if (command.MakePermanent)
        {
            group.MakePermanent();
        }

        group.SetArchived(command.IsArchived);
        group.SetDefaultOrder(command.OrderedPlayerIds);

        // Reordering and module-owned colors must roll back together.
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await SaveGroupChangesAsync(dbContext, group, cancellationToken);
        if (command.ColorDefaults is not null)
        {
            var store = colorStores.SingleOrDefault(store => store.GameKey == command.ColorDefaults.GameKey);
            if (store is not null)
                await store.ReplaceAsync(dbContext, group.Id, command.ColorDefaults.Colors, cancellationToken);
            else if (command.ColorDefaults.Colors.Values.Any(color => color.HasValue))
                throw new InvalidOperationException("No color storage registered for this game.");
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<EditGroupDetails?> GetAsync(
        Guid groupId,
        string? gameKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(groupId, Guid.Empty);
        await using BoardgameTrackerDbContext dbContext =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var group = await dbContext.Groups
            .AsNoTracking()
            .Where(candidate => candidate.Id == groupId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Name,
                candidate.IsTemporary,
                candidate.IsArchived
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (group is null)
        {
            return null;
        }

        EditGroupPlayer[] players = await (
            from membership in dbContext.GroupPlayers.AsNoTracking()
            join player in dbContext.Players.AsNoTracking()
                on membership.PlayerId equals player.Id
            where membership.GroupId == groupId
            orderby membership.DefaultOrder
            select new EditGroupPlayer(
                player.Id,
                player.Name,
                player.IsGuest,
                membership.DefaultOrder))
            .ToArrayAsync(cancellationToken);

        var store = colorStores.SingleOrDefault(store => store.GameKey == gameKey);
        IReadOnlyDictionary<Guid, PlayerColor?> colors = store is null
            ? new Dictionary<Guid, PlayerColor?>()
            : await store.ReadAsync(dbContext, groupId, cancellationToken);
        players = [.. players.Select(player => player with { DefaultColor = colors.GetValueOrDefault(player.PlayerId) })];

        return new EditGroupDetails(
            group.Id,
            group.Name,
            group.IsTemporary,
            group.IsArchived,
            players);
    }

    private static async Task SaveGroupChangesAsync(
        BoardgameTrackerDbContext dbContext, Group group, CancellationToken cancellationToken)
    {
        int updatedGroups = await dbContext.Groups
            .Where(candidate => candidate.Id == group.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(candidate => candidate.Name, group.Name)
                    .SetProperty(candidate => candidate.IsTemporary, group.IsTemporary)
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

/// <summary>Game-independent changes to fixed-membership group metadata and order.</summary>
public sealed record EditGroupCommand(
    Guid GroupId,
    string Name,
    bool MakePermanent,
    bool IsArchived,
    IReadOnlyList<Guid> OrderedPlayerIds,
    GroupColorDefaults? ColorDefaults = null);

/// <summary>
/// Application read model containing exactly the persisted group state needed by the edit screen.
/// </summary>
public sealed record EditGroupDetails(
    Guid Id,
    string Name,
    bool IsTemporary,
    bool IsArchived,
    IReadOnlyList<EditGroupPlayer> Players);

/// <summary>
/// Projected player data used while editing a group.
/// </summary>
public sealed record EditGroupPlayer(
    Guid PlayerId,
    string Name,
    bool IsGuest,
    int DefaultOrder,
    PlayerColor? DefaultColor = null);

using BoardgameTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Groups;

public sealed class FindGroupDuplicatesQuery(IDbContextFactory<BoardgameTrackerDbContext> contextFactory)
{
    public async Task<IReadOnlyList<GroupDuplicate>> FindDuplicatesAsync(
        IReadOnlyList<CreateGroupPlayerInput> players, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(players);
        if (players.Count == 0)
            throw new ArgumentException("A group must contain at least one player.", nameof(players));
        Guid[] ids = players.Where(player => player.ExistingPlayerId.HasValue).Select(player => player.ExistingPlayerId!.Value).ToArray();
        if (ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("Existing players must have distinct nonempty IDs.", nameof(players));
        // Unsaved players are new identities, even if their draft names match local names.
        if (ids.Length != players.Count)
            return [];

        await using BoardgameTrackerDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Groups.AsNoTracking()
            .Where(group => group.Players.Count == ids.Length && group.Players.All(player => ids.Contains(player.PlayerId)))
            .OrderBy(group => group.Name).ThenBy(group => group.Id)
            .Select(group => new GroupDuplicate(group.Id, group.Name, group.IsArchived,
                group.Players.OrderBy(player => player.DefaultOrder).Select(player => player.PlayerId).ToArray()))
            .ToArrayAsync(cancellationToken);
    }
}

public sealed record GroupDuplicate(Guid Id, string Name, bool IsArchived, IReadOnlyList<Guid> OrderedPlayerIds);

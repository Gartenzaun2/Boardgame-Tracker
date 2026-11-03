using BoardgameTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Groups;

public sealed class ListManagedGroupsQuery(IDbContextFactory<BoardgameTrackerDbContext> factory)
{
    public async Task<IReadOnlyList<ManagedGroup>> ListAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        await using BoardgameTrackerDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        var groups = await db.Groups.AsNoTracking().Where(group => includeArchived || !group.IsArchived)
            .OrderBy(group => group.Name).ThenBy(group => group.Id)
            .Select(group => new { group.Id, group.Name, group.IsArchived }).ToArrayAsync(cancellationToken);
        Guid[] ids = groups.Select(group => group.Id).ToArray();
        var members = await (from membership in db.GroupPlayers.AsNoTracking()
                             join player in db.Players.AsNoTracking() on membership.PlayerId equals player.Id
                             where ids.Contains(membership.GroupId)
                             orderby membership.DefaultOrder
                             select new { membership.GroupId, player.Name }).ToArrayAsync(cancellationToken);
        return groups.Select(group => new ManagedGroup(group.Id, group.Name, group.IsArchived,
            members.Where(member => member.GroupId == group.Id).Select(member => member.Name).ToArray())).ToArray();
    }
}
public sealed record ManagedGroup(Guid Id, string Name, bool IsArchived, IReadOnlyList<string> PlayerNames);

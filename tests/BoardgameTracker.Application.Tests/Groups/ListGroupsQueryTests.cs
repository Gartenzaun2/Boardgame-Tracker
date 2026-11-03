using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Groups;

public sealed class ListGroupsQueryTests
{
    [Fact]
    public async Task ListGroupsAsync_ShowsRegularGroupsAndOnlyFiltersArchiveState()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Group archived = new("Archived", [database.First.Id]);
        archived.SetArchived(true);
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            seed.Groups.Add(archived);
            await seed.SaveChangesAsync(token);
        }
        string gameKey;
        await using (BoardgameTrackerDbContext read = database.CreateDbContext())
            gameKey = await read.Games.Where(game => game.Id == database.GameId).Select(game => game.Key).SingleAsync(token);
        ListGroupsQuery query = new(database);
        GroupListItem active = Assert.Single(await query.ListGroupsAsync(gameKey, cancellationToken: token));
        Assert.Equal(database.Group.Id, active.Id);
        Assert.False(active.IsArchived);
        Assert.Equal(new[] { database.First.Name, database.Second.Name }, active.PlayerNames);
        IReadOnlyList<GroupListItem> all = await query.ListGroupsAsync(gameKey, includeArchived: true, cancellationToken: token);
        Assert.Equal(2, all.Count);
        Assert.Contains(all, group => group.Id == archived.Id && group.IsArchived);
    }
}

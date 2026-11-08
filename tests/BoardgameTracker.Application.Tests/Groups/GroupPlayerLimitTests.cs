using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Groups;

public sealed class GroupPlayerLimitTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static CreateGroupCommand Command(string name, int count, params Guid[] games) =>
        new(name, Enumerable.Range(1, count).Select(index => new CreateGroupPlayerInput(null, name + index, false)).ToArray(), games);

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public async Task Create_EnforcesLimitWithoutPartialPlayersOrGroup(int count)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        CreateGroupUseCase create = new(database, TestGameLimits.Catalog);
        if (count == 4)
            await create.CreateGroupAsync(Command("New", count, database.GameId), Token);
        else
            await Assert.ThrowsAsync<GroupPlayerLimitExceededException>(() =>
                create.CreateGroupAsync(Command("New", count, database.GameId), Token));
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Equal(count == 4 ? 6 : 2, await verify.Players.CountAsync(Token));
        Assert.Equal(count == 4 ? 2 : 1, await verify.Groups.CountAsync(Token));
        Assert.Equal(count == 4 ? 1 : 0, await verify.GroupGames.CountAsync(Token));
    }

    [Theory]
    [InlineData("add")]
    [InlineData("edit")]
    [InlineData("reuse")]
    [InlineData("reactivate")]
    public async Task LargeIndependentGroup_CannotAcquireAssignmentThroughAnyUseCase(string route)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Guid id = await new CreateGroupUseCase(database, TestGameLimits.Catalog).CreateGroupAsync(Command("Large", 5), Token);
        Guid[] players;
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            Group group = await seed.Groups.Include(group => group.Players).SingleAsync(group => group.Id == id, Token);
            group.SetArchived(route == "reactivate");
            players = group.Players.OrderBy(player => player.DefaultOrder).Select(player => player.PlayerId).ToArray();
            await seed.SaveChangesAsync(Token);
        }
        Task Save() => route switch
        {
            "add" => new AddGroupGameUseCase(database, TestGameLimits.Catalog).AddGroupGameAsync(id, database.GameId, Token),
            "edit" => new EditGroupUseCase(database, TestGameLimits.Catalog).UpdateGroupAsync(new(id, "Changed", true, players.Reverse().ToArray(), [database.GameId]), Token),
            _ => new ReuseGroupUseCase(database, TestGameLimits.Catalog).ReuseGroupAsync(new(id, players, [database.GameId],
                route == "reactivate" ? GroupReuseAction.Reactivate : GroupReuseAction.UseExisting), Token)
        };
        await Assert.ThrowsAsync<GroupPlayerLimitExceededException>(Save);
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Group saved = await verify.Groups.Include(group => group.Players).SingleAsync(group => group.Id == id, Token);
        Assert.Equal("Large", saved.Name);
        Assert.Equal(route == "reactivate", saved.IsArchived);
        Assert.Equal(players, saved.Players.OrderBy(player => player.DefaultOrder).Select(player => player.PlayerId));
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
        Assert.DoesNotContain(await new ListGroupsQuery(database, TestGameLimits.Catalog).GetAvailableGroupsAsync("ark-nova", Token), group => group.Id == id);
        Assert.Empty((await new EditGroupUseCase(database, TestGameLimits.Catalog).GetAsync(id, Token))!.Games);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("edit")]
    [InlineData("reuse")]
    [InlineData("reactivate")]
    public async Task FourStoredMembers_CanAcquireAssignmentThroughAnyUseCase(string route)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Guid id = await new CreateGroupUseCase(database, TestGameLimits.Catalog).CreateGroupAsync(Command("Four", 4), Token);
        Guid[] players;
        await using (BoardgameTrackerDbContext seed = database.CreateDbContext())
        {
            Group group = await seed.Groups.Include(group => group.Players).SingleAsync(group => group.Id == id, Token);
            group.SetArchived(route == "reactivate");
            players = group.Players.OrderBy(player => player.DefaultOrder).Select(player => player.PlayerId).ToArray();
            await seed.SaveChangesAsync(Token);
        }
        switch (route)
        {
            case "add":
                await new AddGroupGameUseCase(database, TestGameLimits.Catalog).AddGroupGameAsync(id, database.GameId, Token);
                break;
            case "edit":
                await new EditGroupUseCase(database, TestGameLimits.Catalog).UpdateGroupAsync(new(id, "Edited", false, players, [database.GameId]), Token);
                break;
            default:
                await new ReuseGroupUseCase(database, TestGameLimits.Catalog).ReuseGroupAsync(new(id, players, [database.GameId],
                    route == "reactivate" ? GroupReuseAction.Reactivate : GroupReuseAction.UseExisting), Token);
                break;
        }
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        GroupGame assignment = Assert.Single(await verify.GroupGames.ToArrayAsync(Token));
        Assert.Equal(id, assignment.GroupId);
        Assert.Equal(database.GameId, assignment.GameId);
        Assert.False((await verify.Groups.SingleAsync(group => group.Id == id, Token)).IsArchived);
    }

    [Fact]
    public async Task Factory_RejectsOversizedAssignmentWithoutPersistingDraftRows()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
        {
            Player[] players = Enumerable.Range(1, 5).Select(index => new Player("Direct" + index)).ToArray();
            Group group = new("Direct", players.Select(player => player.Id).ToArray());
            db.Players.AddRange(players);
            db.Groups.Add(group);
            Game game = await db.Games.SingleAsync(game => game.Id == database.GameId, Token);
            Assert.Throws<GroupPlayerLimitExceededException>(() =>
                db.GroupGames.Add(GroupGame.Create(group, game, TestGameLimits.Catalog.GetLimit(game.Key))));
        }
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Equal(2, await verify.Players.CountAsync(Token));
        Assert.Single(await verify.Groups.ToArrayAsync(Token));
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
    }

    [Theory]
    [InlineData("add")]
    [InlineData("edit")]
    [InlineData("reuse")]
    [InlineData("reactivate")]
    public async Task ExistingAssignment_DoesNotResolveLimitAgain(string route)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync(archived: route == "reactivate");
        await new AddGroupGameUseCase(database, TestGameLimits.Catalog).AddGroupGameAsync(database.Group.Id, database.GameId, Token);
        GamePlayerLimitCatalog noLimits = new([]);
        Guid[] players = [database.First.Id, database.Second.Id];
        switch (route)
        {
            case "add":
                await new AddGroupGameUseCase(database, noLimits).AddGroupGameAsync(database.Group.Id, database.GameId, Token);
                break;
            case "edit":
                await new EditGroupUseCase(database, noLimits).UpdateGroupAsync(new(database.Group.Id, "Edited", false,
                    players.Reverse().ToArray(), [database.GameId]), Token);
                break;
            default:
                await new ReuseGroupUseCase(database, noLimits).ReuseGroupAsync(new(database.Group.Id, players, [database.GameId],
                    route == "reactivate" ? GroupReuseAction.Reactivate : GroupReuseAction.UseExisting), Token);
                break;
        }
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        GroupGame assignment = Assert.Single(await verify.GroupGames.ToArrayAsync(Token));
        Assert.Equal(database.Group.Id, assignment.GroupId);
        Assert.Equal(database.GameId, assignment.GameId);
        Assert.False((await verify.Groups.SingleAsync(Token)).IsArchived);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Persistence_MaterializesFactoryAssignmentAfterEitherSavePath(bool synchronous)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
        {
            Game game = await db.Games.SingleAsync(game => game.Id == database.GameId, Token);
            db.GroupGames.Add(GroupGame.Create(database.Group, game, TestGameLimits.Catalog.GetLimit(game.Key)));
            if (synchronous)
                db.SaveChanges();
            else
                await db.SaveChangesAsync(Token);
        }
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        GroupGame assignment = Assert.Single(await verify.GroupGames.ToArrayAsync(Token));
        Assert.Equal(database.Group.Id, assignment.GroupId);
        Assert.Equal(database.GameId, assignment.GameId);
    }

    [Fact]
    public async Task Add_RejectsUnknownLimitWithoutPersistingAssignment()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Game game = new("unregistered");
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
        {
            db.Games.Add(game);
            await db.SaveChangesAsync(Token);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => new AddGroupGameUseCase(database, TestGameLimits.Catalog)
            .AddGroupGameAsync(database.Group.Id, game.Id, Token));
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
    }

    [Fact]
    public async Task Reuse_UsesStoredMembershipAndRejectsMismatchedDraft()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Guid id = await new CreateGroupUseCase(database, TestGameLimits.Catalog).CreateGroupAsync(Command("Large", 5), Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReuseGroupUseCase(database, TestGameLimits.Catalog)
            .ReuseGroupAsync(new(id, [database.First.Id], [database.GameId], GroupReuseAction.UseExisting), Token));
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
    }
}

using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;

namespace BoardgameTracker.Application.Tests.Groups;

public sealed class GroupManagementTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_StoresNewPlayersAndZeroOrMultipleDistinctAssignments(bool withGames)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Game other = await AddGameAsync(database);
        Guid[] gameIds = withGames ? [database.GameId, other.Id, database.GameId] : [];
        CreateGroupUseCase useCase = new(database);
        Guid id = await useCase.CreateGroupAsync(new("Created", [new(database.First.Id, "Ignored draft name", false), new(null, "Guest", true)], gameIds), Token);
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Group saved = await verify.Groups.Include(group => group.Players).SingleAsync(group => group.Id == id, Token);
        Assert.Equal(database.First.Id, saved.Players.Single(player => player.DefaultOrder == 1).PlayerId);
        Guid newPlayerId = saved.Players.Single(member => member.DefaultOrder == 2).PlayerId;
        Assert.True((await verify.Players.SingleAsync(player => player.Id == newPlayerId, Token)).IsGuest);
        Assert.Equal(3, await verify.Players.CountAsync(Token));
        Assert.Equal(withGames ? 2 : 0, await verify.GroupGames.CountAsync(assignment => assignment.GroupId == id, Token));
        Assert.Empty(await verify.ArkNovaGroupPlayers.ToArrayAsync(Token));
        Assert.Contains(await useCase.GetSelectableGamesAsync(Token), game => game.Id == other.Id && game.Key == other.Key);
    }

    [Fact]
    public async Task Create_LateAssignmentFailureRollsBackPlayersGroupAndMemberships()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectAssignment BEFORE INSERT ON GroupGames BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        await Assert.ThrowsAsync<DbUpdateException>(() => new CreateGroupUseCase(database).CreateGroupAsync(
            new("Created", [new(null, "Guest", true)], [database.GameId]), Token));
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Equal(2, await verify.Players.CountAsync(Token));
        Assert.Single(await verify.Groups.ToArrayAsync(Token));
        Assert.Equal(2, await verify.GroupPlayers.CountAsync(Token));
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_RejectsMissingOrEmptyGameIdsWithoutWrites(bool empty)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        CreateGroupCommand command = new("Created", [new(null, "Guest", true)], [empty ? Guid.Empty : Guid.NewGuid()]);
        if (empty)
            await Assert.ThrowsAsync<ArgumentException>(() => new CreateGroupUseCase(database).CreateGroupAsync(command, Token));
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => new CreateGroupUseCase(database).CreateGroupAsync(command, Token));
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Equal(2, await verify.Players.CountAsync(Token));
        Assert.Single(await verify.Groups.ToArrayAsync(Token));
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
    }

    [Fact]
    public async Task Duplicates_UsesExactPlayerIdsRegardlessOfOrderNameOrArchiveAndReturnsAllMatches()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Group duplicate = new("Different name", [database.Second.Id, database.First.Id]);
        duplicate.SetArchived(true);
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
        {
            db.Groups.AddRange(duplicate, new Group("Subset", [database.First.Id]));
            await db.SaveChangesAsync(Token);
        }
        FindGroupDuplicatesQuery query = new(database);
        IReadOnlyList<GroupDuplicate> matches = await query.FindDuplicatesAsync(
            [new(database.Second.Id, "Ignored", false), new(database.First.Id, "Ignored too", false)], Token);
        Assert.Equal(2, matches.Count);
        Assert.Contains(matches, group => group.Id == database.Group.Id && !group.IsArchived);
        GroupDuplicate archived = Assert.Single(matches, group => group.Id == duplicate.Id);
        Assert.True(archived.IsArchived);
        Assert.Equal(new[] { database.Second.Id, database.First.Id }, archived.OrderedPlayerIds);
        Assert.Empty(await query.FindDuplicatesAsync([new(null, database.Second.Name, false), new(database.First.Id, database.First.Name, false)], Token));
        Assert.Empty(await query.FindDuplicatesAsync([new(database.First.Id, "Ignored", false), new(Guid.NewGuid(), "Other", false)], Token));
    }

    [Fact]
    public async Task Create_ExplicitlyAllowsSecondGroupWithIdenticalMembership()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        CreateGroupPlayerInput[] players = [new(database.Second.Id, "Ben", false), new(database.First.Id, "Anna", false)];
        Assert.Single(await new FindGroupDuplicatesQuery(database).FindDuplicatesAsync(players, Token));
        Guid id = await new CreateGroupUseCase(database).CreateGroupAsync(new("Deliberate duplicate", players, []), Token);
        Assert.NotEqual(database.Group.Id, id);
        Assert.Equal(2, (await new FindGroupDuplicatesQuery(database).FindDuplicatesAsync(players, Token)).Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReuseOrReactivate_RetainsIdentityMetadataOrderAndOnlyUnionsAssignments(bool archived)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync(archived: archived);
        Game other = await AddGameAsync(database);
        await new AddGroupGameUseCase(database).AddGroupGameAsync(database.Group.Id, database.GameId, Token);
        ReuseGroupUseCase useCase = new(database);
        ReuseGroupCommand command = new(database.Group.Id, [database.Second.Id, database.First.Id], [other.Id, other.Id],
            archived ? GroupReuseAction.Reactivate : GroupReuseAction.UseExisting);
        Assert.Equal(database.Group.Id, await useCase.ReuseGroupAsync(command, Token));
        await useCase.ReuseGroupAsync(command, Token);
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Group saved = await verify.Groups.Include(group => group.Players).SingleAsync(Token);
        Assert.Equal(database.Group.Id, saved.Id);
        Assert.Equal(database.Group.Name, saved.Name);
        Assert.False(saved.IsArchived);
        Assert.Equal(new[] { database.First.Id, database.Second.Id }, saved.Players.OrderBy(player => player.DefaultOrder).Select(player => player.PlayerId));
        Assert.Equal(2, await verify.Players.CountAsync(Token));
        Guid[] assigned = await verify.GroupGames.Select(assignment => assignment.GameId).ToArrayAsync(Token);
        Assert.True(assigned.ToHashSet().SetEquals([database.GameId, other.Id]));
    }

    [Fact]
    public async Task Reuse_RequiresExplicitReactivationAndExactMembers()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync(archived: true);
        ReuseGroupUseCase useCase = new(database);
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ReuseGroupAsync(
            new(database.Group.Id, [database.First.Id, database.Second.Id], [database.GameId], GroupReuseAction.UseExisting), Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ReuseGroupAsync(
            new(database.Group.Id, [database.First.Id], [database.GameId], GroupReuseAction.Reactivate), Token));
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.True((await verify.Groups.SingleAsync(Token)).IsArchived);
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
        Assert.Equal(2, await verify.Players.CountAsync(Token));
    }

    [Fact]
    public async Task Reactivate_AssignmentFailureRollsBackArchiveState()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync(archived: true);
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectAssignment BEFORE INSERT ON GroupGames BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        await Assert.ThrowsAsync<DbUpdateException>(() => new ReuseGroupUseCase(database).ReuseGroupAsync(
            new(database.Group.Id, [database.First.Id, database.Second.Id], [database.GameId], GroupReuseAction.Reactivate), Token));
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.True((await verify.Groups.SingleAsync(Token)).IsArchived);
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
    }

    [Fact]
    public async Task Edit_ReplacesSelectionAndPreservesHistory()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Game other = await AddGameAsync(database);
        await new AddGroupGameUseCase(database).AddGroupGameAsync(database.Group.Id, database.GameId, Token);
        GameSession session = await database.AddSessionAsync(scoring: true);
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
        {
            (await db.GameSessions.SingleAsync(Token)).Complete(session.EndedAt!.Value);
            await db.SaveChangesAsync(Token);
        }
        await new EditGroupUseCase(database).UpdateGroupAsync(new(database.Group.Id, "Edited", true,
            [database.Second.Id, database.First.Id], [other.Id, other.Id]), Token);
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Assert.Equal(other.Id, (await verify.GroupGames.SingleAsync(Token)).GameId);
        Group saved = await verify.Groups.Include(group => group.Players).SingleAsync(Token);
        Assert.Equal("Edited", saved.Name);
        Assert.True(saved.IsArchived);
        Assert.Equal(database.Second.Id, saved.Players.Single(player => player.DefaultOrder == 1).PlayerId);
        GameSession historical = await verify.GameSessions.Include(game => game.Players).SingleAsync(Token);
        Assert.Equal(database.GameId, historical.GameId);
        Assert.Equal(database.Group.Id, historical.GroupId);
        Assert.Equal(GameSessionStatus.Completed, historical.Status);
        Assert.Equal(database.First.Id, historical.Players.Single(player => player.Order == 1).PlayerId);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Edit_RechecksSessionsAfterReadAndRollsBackAllChanges(bool scoring, bool archive)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Game other = await AddGameAsync(database);
        await new AddGroupGameUseCase(database).AddGroupGameAsync(database.Group.Id, database.GameId, Token);
        EditGroupUseCase useCase = new(database);
        EditGroupDetails before = (await useCase.GetAsync(database.Group.Id, Token))!;
        Assert.True(before.CanArchive);
        await database.AddSessionAsync(scoring);
        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.UpdateGroupAsync(new(database.Group.Id,
            "Must roll back", archive, [database.Second.Id, database.First.Id], [other.Id]), Token));
        EditGroupDetails after = (await useCase.GetAsync(database.Group.Id, Token))!;
        Assert.Equal(before.Name, after.Name);
        Assert.False(after.IsArchived);
        Assert.Equal(before.Players, after.Players);
        Assert.False(after.CanArchive);
        Assert.Contains(after.Games, game => game.Id == database.GameId && game.IsAssigned && !game.CanRemove);
        Assert.Contains(after.Games, game => game.Id == other.Id && !game.IsAssigned && game.CanRemove);
    }

    [Fact]
    public async Task Edit_LateAssignmentFailureRollsBackMetadataOrderAndRemovedAssignments()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        Game other = await AddGameAsync(database);
        await new AddGroupGameUseCase(database).AddGroupGameAsync(database.Group.Id, database.GameId, Token);
        await using (BoardgameTrackerDbContext db = database.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectAssignment BEFORE INSERT ON GroupGames BEGIN SELECT RAISE(ABORT, 'test'); END;", Token);
        await Assert.ThrowsAsync<DbUpdateException>(() => new EditGroupUseCase(database).UpdateGroupAsync(
            new(database.Group.Id, "Edited", true, [database.Second.Id, database.First.Id], [other.Id]), Token));
        await using BoardgameTrackerDbContext verify = database.CreateDbContext();
        Group group = await verify.Groups.Include(group => group.Players).SingleAsync(Token);
        Assert.Equal(database.Group.Name, group.Name);
        Assert.False(group.IsArchived);
        Assert.Equal(database.First.Id, group.Players.Single(player => player.DefaultOrder == 1).PlayerId);
        Assert.Equal(database.GameId, (await verify.GroupGames.SingleAsync(Token)).GameId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Edit_RejectsMissingOrEmptyGameIdsAtomically(bool empty)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        EditGroupCommand command = new(database.Group.Id, "Edited", true, [database.Second.Id, database.First.Id], [empty ? Guid.Empty : Guid.NewGuid()]);
        if (empty)
            await Assert.ThrowsAsync<ArgumentException>(() => new EditGroupUseCase(database).UpdateGroupAsync(command, Token));
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => new EditGroupUseCase(database).UpdateGroupAsync(command, Token));
        EditGroupDetails group = (await new EditGroupUseCase(database).GetAsync(database.Group.Id, Token))!;
        Assert.Equal(database.Group.Name, group.Name);
        Assert.False(group.IsArchived);
        Assert.Equal(database.First.Id, group.Players[0].PlayerId);
    }

    private static async Task<Game> AddGameAsync(SliceDatabase database)
    {
        Game game = new("other-game", "other-game", "other-game", 1, 8);
        await using BoardgameTrackerDbContext db = database.CreateDbContext();
        db.Games.Add(game);
        await db.SaveChangesAsync(Token);
        return game;
    }
}

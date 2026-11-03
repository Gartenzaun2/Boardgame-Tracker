using BoardgameTracker.App.Features.Groups;
using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.Players.EditPlayer;
using BoardgameTracker.App.Features.Players.ListPlayers;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Groups;
using BoardgameTracker.Core.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class GroupManagementViewModelTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private sealed class Settings : IAppNavigationSettingsStore
    {
        public AppNavigationSettings Value { get; set; } = AppNavigationSettings.Default;
        public AppNavigationSettings Get() => Value;
        public void Save(AppNavigationSettings settings) => Value = settings;
    }
    private static CreateGroupViewModel Draft(SliceDatabase db, bool management = true)
    {
        CreateGroupViewModel vm = new(new(db), new(db), new(db), [], new Settings { Value = AppNavigationSettings.Default with { SelectedGameKey = "ark-nova" } }, NullLogger<CreateGroupViewModel>.Instance);
        if (management) vm.SetManagementOrigin();
        return vm;
    }
    private static void AddExisting(CreateGroupViewModel vm, Guid id)
    {
        vm.SelectedExistingPlayer = vm.AvailablePlayers.Single(player => player.Id == id);
        vm.AddSelectedPlayerCommand.Execute(null);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_ExistingAndNewPlayersWithZeroOrMultipleGames(bool withGames)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
        {
            seed.Games.Add(new Game("second-game"));
            await seed.SaveChangesAsync(Token);
        }
        CreateGroupViewModel vm = Draft(db);
        await vm.LoadAsync(Token);
        Assert.All(vm.Games, game => Assert.False(game.IsSelected));
        vm.GroupName = "Draft";
        AddExisting(vm, db.First.Id);
        vm.NewPlayerName = "Guest";
        vm.NewPlayerIsGuest = true;
        vm.AddNewPlayerCommand.Execute(null);
        if (withGames) foreach (GroupGameSelection game in vm.Games) game.IsSelected = true;
        int saved = 0;
        vm.SavedAsync = duplicate => { Assert.False(duplicate); saved++; return Task.CompletedTask; };
        await using (BoardgameTrackerDbContext verify = db.CreateDbContext())
        {
            Assert.Equal(2, await verify.Players.CountAsync(Token));
            Assert.Single(await verify.Groups.ToArrayAsync(Token));
        }
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.HasError);
        Assert.Equal(1, saved);
        await using BoardgameTrackerDbContext final = db.CreateDbContext();
        Group group = await final.Groups.Include(group => group.Players).SingleAsync(group => group.Name == "Draft", Token);
        Assert.Equal(2, group.Players.Count);
        Assert.Equal(3, await final.Players.CountAsync(Token));
        Assert.True((await final.Players.SingleAsync(player => player.Name == "Guest", Token)).IsGuest);
        Assert.Equal(withGames ? 2 : 0, await final.GroupGames.CountAsync(game => game.GroupId == group.Id, Token));
    }

    [Fact]
    public async Task CancelledDraft_DoesNotPersistAndGameContextPreselectsOnlyItsGame()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        CreateGroupViewModel vm = Draft(db, management: false);
        await vm.LoadAsync(Token);
        Assert.True(Assert.Single(vm.Games).IsSelected);
        vm.GroupName = "Aborted";
        vm.NewPlayerName = "Unsaved";
        vm.AddNewPlayerCommand.Execute(null);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(2, await verify.Players.CountAsync(Token));
        Assert.Single(await verify.Groups.ToArrayAsync(Token));
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public async Task Duplicates_ShowAllInStableOrderAndBackKeepsEntireDraft(int count)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
        {
            for (int index = 1; index < count; index++)
            {
                Group duplicate = new("Duplicate " + index, [db.Second.Id, db.First.Id]);
                duplicate.SetArchived(true);
                seed.Groups.Add(duplicate);
            }
            await seed.SaveChangesAsync(Token);
        }
        CreateGroupViewModel vm = Draft(db);
        await vm.LoadAsync(Token);
        vm.GroupName = "Kept draft";
        vm.NewPlayerName = "Still typing";
        AddExisting(vm, db.Second.Id);
        AddExisting(vm, db.First.Id);
        vm.Games[0].IsSelected = true;
        int opened = 0;
        vm.ShowDuplicatesAsync = () => { opened++; return Task.CompletedTask; };
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(1, opened);
        Assert.Equal(count, vm.Duplicates.Count);
        Assert.All(vm.Duplicates.Where(row => row.IsArchived), row => Assert.Equal("Ben · Anna", row.Members));
        Assert.Equal("Anna · Ben", vm.Duplicates.Single(row => row.Id == db.Group.Id).Members);
        Assert.Equal(vm.Duplicates.OrderBy(row => row.Name, StringComparer.Ordinal).Select(row => row.Id), vm.Duplicates.Select(row => row.Id));
        vm.CancelDuplicates();
        await vm.LoadAsync(Token);
        Assert.Equal("Kept draft", vm.GroupName);
        Assert.Equal("Still typing", vm.NewPlayerName);
        Assert.Equal(new[] { db.Second.Id, db.First.Id }, vm.Players.Select(player => player.PlayerId!.Value));
        Assert.True(vm.Games[0].IsSelected);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(count, await verify.Groups.CountAsync(Token));
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicateAction_ReusesExactlyTappedIdentityAndUnionsAssignments(bool archived)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        Group target = new("Target", [db.Second.Id, db.First.Id]);
        target.SetArchived(archived);
        Game other = new("second-game");
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
        {
            seed.AddRange(target, other);
            seed.GroupGames.Add(new(target.Id, other.Id));
            await seed.SaveChangesAsync(Token);
        }
        CreateGroupViewModel vm = Draft(db);
        await vm.LoadAsync(Token);
        vm.GroupName = "Discarded name";
        AddExisting(vm, db.First.Id);
        AddExisting(vm, db.Second.Id);
        vm.Games.Single(game => game.Id == db.GameId).IsSelected = true;
        await vm.SaveCommand.ExecuteAsync(null);
        int saved = 0;
        vm.SavedAsync = duplicate => { Assert.True(duplicate); saved++; return Task.CompletedTask; };
        await vm.ReuseDuplicateCommand.ExecuteAsync(vm.Duplicates.Single(row => row.Id == target.Id));
        Assert.False(vm.HasError);
        Assert.Equal(1, saved);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Group group = await verify.Groups.Include(group => group.Players).SingleAsync(group => group.Id == target.Id, Token);
        Assert.False(group.IsArchived);
        Assert.Equal("Target", group.Name);
        Assert.Equal(db.Second.Id, group.Players.Single(player => player.DefaultOrder == 1).PlayerId);
        Assert.Equal(2, await verify.GroupGames.CountAsync(game => game.GroupId == target.Id, Token));
        Assert.Equal(2, await verify.Groups.CountAsync(Token));
        Assert.Equal(2, await verify.Players.CountAsync(Token));
    }

    [Fact]
    public async Task DuplicateAction_CanCreateAdditionalGroupAndFailedReuseKeepsOtherCandidates()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        CreateGroupViewModel vm = Draft(db);
        await vm.LoadAsync(Token);
        vm.GroupName = "Additional";
        AddExisting(vm, db.First.Id);
        AddExisting(vm, db.Second.Id);
        await vm.SaveCommand.ExecuteAsync(null);
        await vm.CreateAdditionalGroupCommand.ExecuteAsync(null);
        Assert.False(vm.HasError);
        await using (BoardgameTrackerDbContext verify = db.CreateDbContext())
            Assert.Equal(2, await verify.Groups.CountAsync(Token));
        vm.GroupName = "Another draft";
        await vm.SaveCommand.ExecuteAsync(null);
        DuplicateGroupRow target = vm.Duplicates.Single(row => row.Name == "Additional");
        await using (BoardgameTrackerDbContext change = db.CreateDbContext())
            await change.Groups.Where(group => group.Id == target.Id).ExecuteDeleteAsync(Token);
        await vm.ReuseDuplicateCommand.ExecuteAsync(target);
        Assert.True(vm.HasError);
        Assert.Equal(2, vm.Duplicates.Count);
        await using BoardgameTrackerDbContext final = db.CreateDbContext();
        Assert.Equal(db.Group.Id, (await final.Groups.SingleAsync(Token)).Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Editor_ShowsSessionLocksAndBackendRechecksOnSave(bool scoring)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        await new AddGroupGameUseCase(db).AddGroupGameAsync(db.Group.Id, db.GameId, Token);
        await db.AddSessionAsync(scoring);
        EditGroupViewModel vm = new(new(db), [], NullLogger<EditGroupViewModel>.Instance);
        await vm.LoadAsync(db.Group.Id, Token);
        Assert.False(vm.CanArchive);
        Assert.True(vm.ArchiveLocked);
        GroupGameSelection game = Assert.Single(vm.Games);
        Assert.True(game.IsLocked);
        Assert.NotEmpty(game.LockReason);
        game.IsSelected = false;
        Assert.True(game.IsSelected);
        vm.GroupName = "Must not persist";
        vm.IsArchived = true;
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.HasError);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(db.Group.Name, (await verify.Groups.SingleAsync(Token)).Name);
    }

    [Fact]
    public async Task Editor_ChangesSelectionAndGlobalOrderAtomically()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        await new AddGroupGameUseCase(db).AddGroupGameAsync(db.Group.Id, db.GameId, Token);
        EditGroupViewModel vm = new(new(db), [], NullLogger<EditGroupViewModel>.Instance);
        await vm.LoadAsync(db.Group.Id, Token);
        vm.GroupName = "Edited";
        vm.Games[0].IsSelected = false;
        vm.MovePlayerDownCommand.Execute(vm.Players[0]);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.False(vm.HasError);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Empty(await verify.GroupGames.ToArrayAsync(Token));
        Assert.Equal("Edited", (await verify.Groups.SingleAsync(Token)).Name);
        Assert.Equal(db.Second.Id, (await verify.GroupPlayers.SingleAsync(player => player.DefaultOrder == 1, Token)).PlayerId);
    }

    [Fact]
    public async Task PlayerCreationAndManagementSwitching_DoNotWriteBeforeSave()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        EditPlayerViewModel editor = new(new(db), new(db), NullLogger<EditPlayerViewModel>.Instance);
        editor.InitializeNew();
        editor.Name = "Guest";
        editor.IsGuest = true;
        PlayerListViewModel list = new(new(db), new(db), NullLogger<PlayerListViewModel>.Instance);
        await list.LoadAsync(Token);
        list.SelectGroups();
        Assert.True(list.ShowGroups);
        Assert.Single(list.Groups);
        list.SelectPlayers();
        Assert.True(list.ShowPlayers);
        await using (BoardgameTrackerDbContext verify = db.CreateDbContext())
            Assert.Equal(2, await verify.Players.CountAsync(Token));
        Assert.True(await editor.SaveAsync(Token));
        await using BoardgameTrackerDbContext final = db.CreateDbContext();
        Assert.True((await final.Players.SingleAsync(player => player.Name == "Guest", Token)).IsGuest);
        Assert.Null(typeof(PlayerListViewModel).GetMethod("OpenLastGameSessionAsync"));
        Assert.Null(typeof(ManagedGroupRow).GetProperty("LastPlayedAt"));
    }
}

using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.Players.EditPlayer;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.Application.Games;
using BoardgameTracker.Application.Groups;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Application.Tests.Persistence;
using BoardgameTracker.Core.GameSessions;
using BoardgameTracker.Core.Games;
using BoardgameTracker.Core.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class PlayNavigationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private sealed class Handler(string key) : IGameSessionNavigationHandler
    {
        public string GameKey => key;
        public string DisplayName => "Localized " + key;
        public string BadgeText => "Badge";
        public List<(Guid Id, Guid? Session)> Opened { get; } = [];
        public Task OpenGameSessionAsync(Guid gameSessionId) => Task.CompletedTask;
        public Task OpenGroupAsync(Guid groupId, Guid? session, GameSessionStatus? status)
        {
            Opened.Add((groupId, session));
            return Task.CompletedTask;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task PlayRoot_ResolvesCurrentOpenGamesAndKeepsGameListBelowGroups(int scenario)
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        var store = new MainSectionNavigationTests.SettingsStore();
        var shell = MainSectionNavigationTests.CreateShell(store, db);
        Assert.Equal(new[] { "GamesPage", "HistoryPage", "PlayerListPage", "AppSettingsPage" }, shell.CurrentItem.Items.Select(tab => tab.Route));
        await shell.NavigateToMainSectionAsync(AppMainSection.Play);
        Assert.Single(shell.CurrentItem.CurrentItem.Navigation.NavigationStack);
        if (scenario > 0)
            await db.AddSessionAsync(scoring: true);
        if (scenario > 1)
        {
            await using BoardgameTrackerDbContext seed = db.CreateDbContext();
            Group second = new("Another group", [db.First.Id]);
            seed.Groups.Add(second);
            Guid gameId = db.GameId;
            if (scenario == 3)
            {
                Game other = new("another-game");
                seed.Games.Add(other);
                gameId = other.Id;
            }
            seed.GameSessions.Add(new(gameId, second.Id, DateTimeOffset.UtcNow, [db.First.Id]));
            await seed.SaveChangesAsync(Token);
        }
        await shell.NavigateFromMainTabTapAsync(shell.CurrentItem.Items[0]);
        bool expectGroups = scenario is 1 or 2;
        Assert.Equal(expectGroups ? 2 : 1, shell.CurrentItem.CurrentItem.Navigation.NavigationStack.Count);
        Assert.Equal(expectGroups ? "GroupListPage" : "//GamesPage", shell.Calls[^1].Route);
        Assert.Null(store.Value.LastGameSessionId);
        if (expectGroups)
        {
            Assert.Equal("ark-nova", shell.Calls[^1].Parameters[NavigationParameters.GameKey]);
            await shell.GoToAsync("..");
            Assert.Single(shell.CurrentItem.CurrentItem.Navigation.NavigationStack);
            Assert.Equal("GamesPage", shell.CurrentItem.CurrentItem.Route);
        }
    }

    [Fact]
    public async Task PreparationCaller_IsGameGroupsRatherThanGameListRoot()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        var shell = MainSectionNavigationTests.CreateShell(new(), db);
        await shell.NavigateToGroupsForGameAsync("ark-nova");
        Page groupPage = shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1];
        await shell.GoToAsync("Preparation");
        shell.CaptureGameSessionCaller(useRoot: true);
        await shell.GoToAsync("Running");
        await shell.GoToAsync("Scoring");
        await shell.ReturnToGameSessionCallerAsync();
        Assert.Same(groupPage, shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
        Assert.Equal(2, shell.CurrentItem.CurrentItem.Navigation.NavigationStack.Count);
    }

    [Fact]
    public async Task AssignedGroups_FilterArchiveAndCurrentLinksAndSortOpenBeforeRecentCompleted()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        Group noLink = new("Not assigned", [db.First.Id]);
        Group archived = new("Archived", [db.First.Id]);
        archived.SetArchived(true);
        Group recent = new("Recent completed", [db.First.Id]);
        await using (BoardgameTrackerDbContext seed = db.CreateDbContext())
        {
            seed.Groups.AddRange(noLink, archived, recent);
            seed.GroupGames.AddRange(new GroupGame(db.Group.Id, db.GameId), new GroupGame(archived.Id, db.GameId), new GroupGame(recent.Id, db.GameId));
            GameSession completed = new(db.GameId, recent.Id, DateTimeOffset.UtcNow, [db.First.Id]);
            completed.BeginScoring(completed.StartedAt);
            completed.Complete(completed.StartedAt);
            seed.GameSessions.Add(completed);
            GameSession historical = new(db.GameId, noLink.Id, DateTimeOffset.UtcNow, [db.First.Id]);
            historical.BeginScoring(historical.StartedAt);
            historical.Complete(historical.StartedAt);
            seed.GameSessions.Add(historical);
            await seed.SaveChangesAsync(Token);
        }
        await db.AddSessionAsync();
        IReadOnlyList<GroupListItem> groups = await new ListGroupsQuery(db).ListGroupsAsync("ark-nova", Token);
        Assert.Equal(new[] { db.Group.Id, recent.Id }, groups.Select(group => group.Id));
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(3, await verify.GameSessions.CountAsync(Token));
    }

    [Fact]
    public async Task AvailableSelection_PersistsAssignmentBeforePreparationAndCancelKeepsIt()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        Handler handler = new("ark-nova");
        GroupListViewModel vm = new(new(db), new(db), new(db), NullLogger<GroupListViewModel>.Instance, [handler]);
        vm.SetGameContext(handler.GameKey);
        await vm.LoadAsync(Token);
        Assert.Empty(vm.Groups);
        Assert.True(vm.CanCreate);
        Assert.True(vm.CanChoose);
        vm.IsChoosingExisting = true;
        await vm.SelectExistingAsync(Assert.Single(vm.AvailableGroups));
        Assert.False(vm.HasError);
        Assert.Equal((db.Group.Id, (Guid?)null), Assert.Single(handler.Opened));
        await vm.LoadAsync(Token); // Returning from an abandoned preparation does not undo the link.
        Assert.Equal(db.Group.Id, Assert.Single(vm.Groups).Id);
        Assert.False(vm.CanChoose);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task GroupSave_UsesActualIdentityAndExplicitOriginForEveryAction(int action)
    {
        foreach (bool playOrigin in new[] { false, true })
        {
            await using SliceDatabase db = await SliceDatabase.CreateAsync(archived: action == 2);
            CreateGroupViewModel vm = new(new(db), new(db), new(db), [], NullLogger<CreateGroupViewModel>.Instance);
            if (playOrigin) vm.SetGameContext("ark-nova");
            else vm.SetManagementOrigin();
            await vm.LoadAsync(Token);
            vm.GroupName = "New identity";
            vm.SelectedExistingPlayer = vm.AvailablePlayers.Single(player => player.Id == db.First.Id);
            vm.AddSelectedPlayerCommand.Execute(null);
            if (action != 0)
            {
                vm.SelectedExistingPlayer = vm.AvailablePlayers.Single(player => player.Id == db.Second.Id);
                vm.AddSelectedPlayerCommand.Execute(null);
            }
            GroupSaveOutcome? saved = null;
            vm.SavedAsync = outcome => { saved = outcome; return Task.CompletedTask; };
            await vm.SaveCommand.ExecuteAsync(null);
            if (action is 1 or 2)
                await vm.ReuseDuplicateCommand.ExecuteAsync(Assert.Single(vm.Duplicates));
            if (action == 3)
                await vm.CreateAdditionalGroupCommand.ExecuteAsync(null);
            Assert.False(vm.HasError);
            Assert.NotNull(saved);
            Assert.Equal(playOrigin ? "ark-nova" : null, saved.GameKey);
            Assert.Equal(action != 0, saved.FromDuplicates);
            if (action is 1 or 2) Assert.Equal(db.Group.Id, saved.GroupId);
            else Assert.NotEqual(db.Group.Id, saved.GroupId);
            await using BoardgameTrackerDbContext verify = db.CreateDbContext();
            Group group = await verify.Groups.SingleAsync(group => group.Id == saved.GroupId, Token);
            Assert.Equal(action is 1 or 2 ? db.Group.Name : "New identity", group.Name);
            Assert.Equal(playOrigin ? 1 : 0, await verify.GroupGames.CountAsync(assignment => assignment.GroupId == saved.GroupId, Token));
        }
    }

    [Fact]
    public async Task NavigationFailureAfterSave_RetriesSameIdentityWithoutWritingAgain()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        CreateGroupViewModel vm = new(new(db), new(db), new(db), [], NullLogger<CreateGroupViewModel>.Instance);
        vm.SetGameContext("ark-nova");
        await vm.LoadAsync(Token);
        vm.GroupName = "One save only";
        vm.NewPlayerName = "Guest";
        vm.AddNewPlayerCommand.Execute(null);
        Guid? id = null;
        int calls = 0;
        vm.SavedAsync = outcome =>
        {
            Assert.Equal(id ?? outcome.GroupId, outcome.GroupId);
            id = outcome.GroupId;
            calls++;
            throw new InvalidOperationException("Navigation unavailable");
        };
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.HasError);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(2, calls);
        await using BoardgameTrackerDbContext verify = db.CreateDbContext();
        Assert.Equal(2, await verify.Groups.CountAsync(Token));
        Assert.Equal(3, await verify.Players.CountAsync(Token));
        Assert.Equal(id, (await verify.Groups.SingleAsync(group => group.Name == "One save only", Token)).Id);
    }

    [Fact]
    public async Task StartupAndPlayerCreation_OnlyFirstSuccessfulPlayerOpensPlay()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        await using (BoardgameTrackerDbContext clear = db.CreateDbContext())
        {
            await clear.Groups.ExecuteDeleteAsync(Token);
            await clear.Players.ExecuteDeleteAsync(Token);
        }
        StartupNavigationQuery query = new(db, new(db));
        StartupNavigation startup = await query.ResolveAsync(AppNavigationSettings.Default with { LastMainSection = AppMainSection.History }, Token);
        Assert.Equal(AppMainSection.Participants, startup.Section);
        Assert.Null(startup.Resume);
        EditPlayerViewModel first = new(new(db), new(db), NullLogger<EditPlayerViewModel>.Instance);
        first.InitializeNew();
        Assert.False(await first.SaveAsync(Token));
        Assert.False(first.ShouldOpenPlay);
        first.Name = "First";
        Assert.True(await first.SaveAsync(Token));
        Assert.True(first.ShouldOpenPlay);
        Assert.Equal(AppMainSection.Play, (await query.ResolveAsync(AppNavigationSettings.Default, Token)).Section);
        EditPlayerViewModel second = new(new(db), new(db), NullLogger<EditPlayerViewModel>.Instance);
        second.InitializeNew();
        second.Name = "Second";
        Assert.True(await second.SaveAsync(Token));
        Assert.False(second.ShouldOpenPlay);
        Assert.Equal(AppMainSection.History, (await query.ResolveAsync(AppNavigationSettings.Default with { LastMainSection = AppMainSection.History }, Token)).Section);
    }

    [Fact]
    public async Task Startup_ResumesOnlyOpenSessionsAndFallsBackWhenCompletedOrMissing()
    {
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        GameSession session = await db.AddSessionAsync();
        AppNavigationSettings settings = AppNavigationSettings.Default with { LastGameSessionId = session.Id, LastMainSection = AppMainSection.History };
        StartupNavigationQuery query = new(db, new(db));
        Assert.Equal(session.Id, (await query.ResolveAsync(settings, Token)).Resume!.GameSessionId);
        await using (BoardgameTrackerDbContext change = db.CreateDbContext())
        {
            GameSession persisted = await change.GameSessions.SingleAsync(Token);
            persisted.BeginScoring(persisted.StartedAt);
            persisted.Complete(persisted.StartedAt);
            await change.SaveChangesAsync(Token);
        }
        StartupNavigation completed = await query.ResolveAsync(settings, Token);
        Assert.Null(completed.Resume);
        Assert.Equal(AppMainSection.History, completed.Section);
        Assert.Null((await query.ResolveAsync(settings with { LastGameSessionId = Guid.NewGuid() }, Token)).Resume);
    }

    [Theory]
    [InlineData(1, AppMainSection.Play)]
    [InlineData(3, AppMainSection.History)]
    [InlineData(4, AppMainSection.Settings)]
    [InlineData(5, AppMainSection.Participants)]
    public void CurrentMainSections_ResolveStoredValues(int stored, AppMainSection expected) =>
        Assert.Equal(expected, AppMainSections.FromStoredValue(stored));

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task InvalidMainSections_FallBackToPlay(int? stored)
    {
        Assert.Null(AppMainSections.FromStoredValue(stored));
        await using SliceDatabase db = await SliceDatabase.CreateAsync();
        StartupNavigationQuery query = new(db, new(db));
        AppNavigationSettings settings = AppNavigationSettings.Default with
        {
            LastMainSection = stored.HasValue ? (AppMainSection)stored.Value : null
        };
        Assert.Equal(AppMainSection.Play, (await query.ResolveAsync(settings, Token)).Section);
    }
}

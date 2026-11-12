using BoardgameTracker.App;
using BoardgameTracker.Application.Persistence;
using BoardgameTracker.Application.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class MainSectionNavigationTests
{
    [Theory]
    [InlineData(AppMainSection.Play)]
    [InlineData(AppMainSection.History)]
    [InlineData(AppMainSection.Participants)]
    public async Task FailedTabTap_PreservesSessionCallerAndSettings(AppMainSection target)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        var (shell, store, caller) = await CreateSessionContextAsync(database);
        AppNavigationSettings before = store.Value;
        Page session = shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1];
        shell.FailNextNavigation = NavigationFailurePoint.Before;
        await Assert.ThrowsAsync<InvalidOperationException>(() => shell.NavigateToMainSectionAsync(target));
        Assert.Equal(before, store.Value);
        Assert.Same(session, shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
        await shell.ReturnToGameSessionCallerAsync();
        Assert.Same(caller, shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
    }

    [Fact]
    public async Task FailedTabTap_RetryResetsRootAndReplacesPreviousCaller()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        var (shell, store, _) = await CreateSessionContextAsync(database);
        Tab target = shell.CurrentItem.Items.Single(tab => tab.Route == "HistoryPage");
        shell.FailNextNavigation = NavigationFailurePoint.Before;
        await Assert.ThrowsAsync<InvalidOperationException>(() => shell.NavigateFromMainTabTapAsync(target));
        await shell.NavigateFromMainTabTapAsync(target);
        Assert.Equal(AppMainSection.History, store.Value.LastMainSection);
        Assert.Null(store.Value.LastGameSessionId);
        Assert.Single(target.Navigation.NavigationStack);
        await shell.GoToAsync("AnotherSession");
        await shell.ReturnToGameSessionCallerAsync();
        Assert.Single(target.Navigation.NavigationStack);
    }

    [Fact]
    public async Task TabNavigationErrorAfterArrival_RecordsReachedRootWithoutRollback()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        var (shell, store, _) = await CreateSessionContextAsync(database);
        shell.FailNextNavigation = NavigationFailurePoint.After;
        await Assert.ThrowsAsync<InvalidOperationException>(() => shell.NavigateToMainSectionAsync(AppMainSection.History));
        Assert.Equal("HistoryPage", shell.CurrentItem.CurrentItem.Route);
        Assert.Single(shell.CurrentItem.CurrentItem.Navigation.NavigationStack);
        Assert.Equal(AppMainSection.History, store.Value.LastMainSection);
        Assert.Null(store.Value.LastGameSessionId);
    }

    [Theory]
    [InlineData(NavigationFailurePoint.Before)]
    [InlineData(NavigationFailurePoint.AfterRoot)]
    [InlineData(NavigationFailurePoint.After)]
    public async Task FailedGameSwitch_RecordsOnlyTheContextActuallyReached(NavigationFailurePoint point)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        var (shell, store, caller) = await CreateSessionContextAsync(database);
        AppNavigationSettings before = store.Value;
        string otherGame = "manual:" + Guid.NewGuid().ToString();
        shell.FailNextNavigation = point;
        await Assert.ThrowsAsync<InvalidOperationException>(() => shell.NavigateToGroupsForGameAsync(otherGame));
        if (point == NavigationFailurePoint.Before)
        {
            Assert.Equal(before, store.Value);
            await shell.ReturnToGameSessionCallerAsync();
            Assert.Same(caller, shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
        }
        else
        {
            Assert.Equal(AppMainSection.Play, store.Value.LastMainSection);
            Assert.Null(store.Value.LastGameSessionId);
            Assert.Equal(point == NavigationFailurePoint.After ? otherGame : before.SelectedGameKey, store.Value.SelectedGameKey);
            Assert.Equal(point == NavigationFailurePoint.After ? 2 : 1, shell.CurrentItem.CurrentItem.Navigation.NavigationStack.Count);
        }
        await shell.NavigateToGroupsForGameAsync(otherGame);
        Assert.Equal(otherGame, store.Value.SelectedGameKey);
        Assert.Equal(AppMainSection.Play, store.Value.LastMainSection);
        Assert.Null(store.Value.LastGameSessionId);
        Assert.Equal(otherGame, Assert.IsType<BoardgameTracker.App.Features.Groups.ListGroups.GroupListPage>(
            shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]).GameKey);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedReturn_RetryUsesRetainedCallerAfterIntermediatePagesWereRemoved(bool callerRemoved)
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        var (shell, store, caller) = await CreateSessionContextAsync(database);
        await shell.GoToAsync("Scoring");
        await shell.GoToAsync("PlayerScoring");
        var navigation = shell.CurrentItem.CurrentItem.Navigation;
        Page visible = navigation.NavigationStack[^1];
        if (callerRemoved)
        {
            navigation.RemovePage(caller);
            caller = navigation.NavigationStack[0];
        }
        AppNavigationSettings before = store.Value;
        shell.FailNextNavigation = NavigationFailurePoint.Before;
        await Assert.ThrowsAsync<InvalidOperationException>(() => shell.ReturnToGameSessionCallerAsync());
        Assert.Same(visible, navigation.NavigationStack[^1]);
        Assert.Same(caller, navigation.NavigationStack[^2]);
        Assert.Equal(before, store.Value);
        await shell.ReturnToGameSessionCallerAsync();
        Assert.Same(caller, navigation.NavigationStack[^1]);
        Assert.Null(store.Value.LastGameSessionId);
    }

    [Fact]
    public async Task ReturnErrorAfterPop_EndsResumeAtTheReachedCaller()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        var (shell, store, caller) = await CreateSessionContextAsync(database);
        shell.FailNextNavigation = NavigationFailurePoint.After;
        await Assert.ThrowsAsync<InvalidOperationException>(() => shell.ReturnToGameSessionCallerAsync());
        Assert.Same(caller, shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
        Assert.Null(store.Value.LastGameSessionId);
        Assert.Equal(AppMainSection.Settings, store.Value.LastMainSection);
    }

    private static async Task<(AppShell Shell, SettingsStore Store, Page Caller)> CreateSessionContextAsync(SliceDatabase database)
    {
        SettingsStore store = new();
        AppShell shell = CreateShell(store, database);
        await shell.NavigateToMainSectionAsync(AppMainSection.Settings);
        await shell.GoToAsync("ManagementCaller");
        Page caller = shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1];
        shell.CaptureGameSessionCaller();
        shell.RememberGameSession(Guid.NewGuid(), ArkNovaGame.Key);
        await shell.GoToAsync("RunningSession");
        return (shell, store, caller);
    }

    internal sealed class SettingsStore : IAppNavigationSettingsStore
    {
        public AppNavigationSettings Value { get; set; } = AppNavigationSettings.Default with { SelectedGameKey = ArkNovaGame.Key };
        public AppNavigationSettings Get() => Value;
        public void Save(AppNavigationSettings settings) => Value = settings;
    }

    internal static AppShell CreateShell(SettingsStore store, IDbContextFactory<BoardgameTrackerDbContext> factory)
    {
        AppShell shell = new(new(), new(), new(), new(), store, new(factory, new TestGameActivationStore()));
        shell.EnableNavigationMemory();
        return shell;
    }

    [Theory]
    [InlineData(AppMainSection.Play, "GamesPage")]
    [InlineData(AppMainSection.History, "HistoryPage")]
    [InlineData(AppMainSection.Settings, "AppSettingsPage")]
    [InlineData(AppMainSection.Participants, "PlayerListPage")]
    public async Task ExplicitTabTapAndReselectionOpenRootAndRememberSection(AppMainSection section, string root)
    {
        SettingsStore store = new();
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        AppShell shell = CreateShell(store, database);
        Tab target = shell.CurrentItem.Items.Single(tab => tab.Route == root);
        target.Navigation.NavigationStackPages.Add(new Page());

        await shell.NavigateFromMainTabTapAsync(target);
        Assert.Equal("//" + root, shell.Calls[^1].Route);
        Assert.Single(target.Navigation.NavigationStack);
        Assert.Equal(section, store.Value.LastMainSection);

        await shell.GoToAsync("NestedPage");
        await shell.NavigateFromMainTabTapAsync(target);
        Assert.Single(target.Navigation.NavigationStack);
        Assert.Equal(section, store.Value.LastMainSection);
        Assert.Null(store.Value.LastGameSessionId);
    }

    [Fact]
    public async Task NavigatedOnExistingSubpageRemembersActualTab()
    {
        SettingsStore store = new();
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        AppShell shell = CreateShell(store, database);
        Tab history = shell.CurrentItem.Items.Single(tab => tab.Route == "HistoryPage");
        history.Navigation.NavigationStackPages.Add(new Page());
        store.Value = store.Value with { LastMainSection = AppMainSection.Settings };
        shell.CurrentItem.CurrentItem = history;
        shell.NotifyNavigated();
        Assert.Equal(AppMainSection.History, store.Value.LastMainSection);
    }

    [Fact]
    public async Task PushingOpenSessionRetainsResumeTargetButRootTapClearsIt()
    {
        SettingsStore store = new();
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        AppShell shell = CreateShell(store, database);
        await shell.NavigateToGroupsForGameAsync(ArkNovaGame.Key);
        Guid id = Guid.NewGuid();
        shell.RememberGameSession(id, ArkNovaGame.Key);
        await shell.GoToAsync("ArkNovaRunningPage");
        Assert.Equal(id, store.Value.LastGameSessionId);
        await shell.NavigateFromMainTabTapAsync(shell.CurrentItem.CurrentItem);
        Assert.Null(store.Value.LastGameSessionId);
    }

    [Theory]
    [InlineData(AppMainSection.Play)]
    [InlineData(AppMainSection.History)]
    [InlineData(AppMainSection.Settings)]
    public async Task SessionTransitionsReturnToActualCaller(AppMainSection section)
    {
        SettingsStore store = new();
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        AppShell shell = CreateShell(store, database);
        if (section == AppMainSection.Play)
            await shell.NavigateToGroupsForGameAsync(ArkNovaGame.Key);
        else
            await shell.NavigateToMainSectionAsync(section);
        if (section == AppMainSection.Settings)
            await shell.GoToAsync("PlayerListPage");
        Page caller = shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1];
        shell.CaptureGameSessionCaller();
        Guid gameSessionId = Guid.NewGuid();
        shell.RememberGameSession(gameSessionId, ArkNovaGame.Key);
        await shell.GoToAsync("Running");
        Assert.Equal(gameSessionId, store.Value.LastGameSessionId);
        await shell.GoToAsync("Scoring");
        Assert.Equal(gameSessionId, store.Value.LastGameSessionId);
        await shell.GoToAsync("PlayerScoring");
        Assert.Equal(gameSessionId, store.Value.LastGameSessionId);
        await shell.GoToAsync("..");
        Assert.Equal(gameSessionId, store.Value.LastGameSessionId);
        await shell.GoToAsync("Result");
        await shell.GoToAsync("Correction");
        await shell.GoToAsync("..");
        await shell.ReturnToGameSessionCallerAsync();
        Assert.Same(caller, shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
        Assert.Null(store.Value.LastGameSessionId);
        Assert.Equal(section, store.Value.LastMainSection);
        Assert.Equal(ArkNovaGame.Key, store.Value.SelectedGameKey);
    }

    [Fact]
    public async Task ReturningWithoutLeavingCallerDoesNotRemoveIt()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        AppShell shell = CreateShell(new SettingsStore(), database);
        await shell.NavigateToMainSectionAsync(AppMainSection.Settings);
        await shell.GoToAsync("PlayerListPage");
        Page caller = shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1];
        shell.CaptureGameSessionCaller();
        await shell.ReturnToGameSessionCallerAsync();
        Assert.Same(caller, shell.CurrentItem.CurrentItem.Navigation.NavigationStack[^1]);
    }

    [Fact]
    public async Task MainTabTapOverridesSessionCaller()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        AppShell shell = CreateShell(new SettingsStore(), database);
        await shell.NavigateToMainSectionAsync(AppMainSection.Settings);
        await shell.GoToAsync("PlayerListPage");
        shell.CaptureGameSessionCaller();
        await shell.GoToAsync("Scoring");
        await shell.NavigateFromMainTabTapAsync(shell.CurrentItem.Items.Single(t => t.Route == "HistoryPage"));
        await shell.GoToAsync("OtherSession");
        await shell.ReturnToGameSessionCallerAsync();
        Assert.Equal("HistoryPage", shell.CurrentItem.CurrentItem.Route);
        Assert.Single(shell.CurrentItem.CurrentItem.Navigation.NavigationStack);
    }

    [Fact]
    public async Task RestoringSectionDoesNotOverwriteRememberedSection()
    {
        SettingsStore store = new();
        store.Value = store.Value with { LastMainSection = AppMainSection.Settings };
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        AppShell shell = CreateShell(store, database);
        await shell.NavigateToMainSectionAsync(AppMainSection.Play, remember: false);
        Assert.Equal(AppMainSection.Settings, store.Value.LastMainSection);
    }
    [Fact]
    public async Task ModuleSettingsEntryOpensItsOwnRoute()
    {
        await using SliceDatabase database = await SliceDatabase.CreateAsync();
        var shell = CreateShell(new(), database);
        Shell.Current = shell;
        BoardgameTracker.App.Navigation.IGameSettingsNavigation entry = new BoardgameTracker.App.Games.ArkNova.Navigation.ArkNovaSettingsNavigation();
        await entry.OpenAsync("ark-nova");
        Assert.Equal("ArkNovaSettingsPage", Assert.Single(shell.Calls).Route);
        Assert.False(Assert.Single(shell.Animations));
        Assert.True(entry.CanHandle("ark-nova"));
        Assert.False(entry.CanHandle("other"));
    }
}

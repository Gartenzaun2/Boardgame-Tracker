using BoardgameTracker.App;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Core.Games.ArkNova;

namespace BoardgameTracker.Application.Tests.Presentation;

public sealed class MainSectionNavigationTests
{
    internal sealed class SettingsStore : IAppNavigationSettingsStore
    {
        public AppNavigationSettings Value { get; set; } = AppNavigationSettings.Default with { SelectedGameKey = ArkNovaGame.Key };
        public AppNavigationSettings Get() => Value;
        public void Save(AppNavigationSettings settings) => Value = settings;
    }

    internal static AppShell CreateShell(SettingsStore store)
    {
        AppShell shell = new(new(), new(), new(), new(), store);
        shell.EnableNavigationMemory();
        return shell;
    }

    [Theory]
    [InlineData(AppMainSection.Games, "GamesPage")]
    [InlineData(AppMainSection.Groups, "GroupListPage")]
    [InlineData(AppMainSection.History, "HistoryPage")]
    [InlineData(AppMainSection.Settings, "AppSettingsPage")]
    public async Task ExplicitTabTapAndReselectionOpenRootAndRememberSection(AppMainSection section, string root)
    {
        SettingsStore store = new();
        AppShell shell = CreateShell(store);
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
    public void NavigatedOnExistingSubpageRemembersActualTab()
    {
        SettingsStore store = new();
        AppShell shell = CreateShell(store);
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
        AppShell shell = CreateShell(store);
        await shell.NavigateToGroupsForGameAsync(ArkNovaGame.Key);
        Guid id = Guid.NewGuid();
        shell.RememberGameSession(id, ArkNovaGame.Key);
        await shell.GoToAsync("ArkNovaRunningPage");
        Assert.Equal(id, store.Value.LastGameSessionId);
        await shell.NavigateFromMainTabTapAsync(shell.CurrentItem.CurrentItem);
        Assert.Null(store.Value.LastGameSessionId);
    }

    [Theory]
    [InlineData(AppMainSection.Groups)]
    [InlineData(AppMainSection.History)]
    [InlineData(AppMainSection.Settings)]
    public async Task SessionTransitionsReturnToActualCaller(AppMainSection section)
    {
        SettingsStore store = new();
        AppShell shell = CreateShell(store);
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
        AppShell shell = CreateShell(new SettingsStore());
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
        AppShell shell = CreateShell(new SettingsStore());
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
        AppShell shell = CreateShell(store);
        await shell.NavigateToMainSectionAsync(AppMainSection.Groups, remember: false);
        Assert.Equal(AppMainSection.Settings, store.Value.LastMainSection);
    }
}

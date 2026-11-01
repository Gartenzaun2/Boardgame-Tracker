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

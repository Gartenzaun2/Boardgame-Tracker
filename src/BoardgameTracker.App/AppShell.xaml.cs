using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Navigation;
using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.History;
using BoardgameTracker.App.Features.Settings.AppSettings;
using BoardgameTracker.App.Features.Settings.MyGames;
using BoardgameTracker.App.Features.Settings.Licenses;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Settings.Navigation;
using BoardgameTracker.Application.Games;
using BoardgameTracker.App.Features.Players.ListPlayers;
using BoardgameTracker.App.Features.Players.EditPlayer;

namespace BoardgameTracker.App;

/// <summary>
/// Defines the MAUI Shell root, persistent main tabs and feature routes.
/// </summary>
public partial class AppShell : Shell
{
    private readonly IAppNavigationSettingsStore _navigationSettingsStore;
    private readonly Tab _gamesTab;
    private readonly PlayOverviewQuery _playOverview;
    private readonly Tab _historyTab;
    private readonly Tab _settingsTab;
    private readonly Tab _participantsTab;
    private bool _isProgrammaticMainNavigation;
    private bool _isNavigationMemoryEnabled;
    private bool _isMainTabTapInProgress;
    private ShellSection? _gameSessionCallerSection;
    private Page? _gameSessionCaller;
    private bool _isReturningToCaller;

    public AppShell(
        GamesPage gamesPage,
        HistoryPage historyPage,
        AppSettingsPage settingsPage,
        PlayerListPage playerListPage,
        IAppNavigationSettingsStore navigationSettingsStore,
        PlayOverviewQuery playOverview)
    {
        InitializeComponent();
        _navigationSettingsStore = navigationSettingsStore;
        _playOverview = playOverview;

        Routing.RegisterRoute(nameof(GroupListPage), typeof(GroupListPage));
        Routing.RegisterRoute(nameof(CreateGroupPage), typeof(CreateGroupPage));
        Routing.RegisterRoute(nameof(EditGroupPage), typeof(EditGroupPage));
        Routing.RegisterRoute(nameof(DuplicateGroupPage), typeof(DuplicateGroupPage));
        Routing.RegisterRoute(nameof(EditPlayerPage), typeof(EditPlayerPage));
        Routing.RegisterRoute(nameof(MyGamesPage), typeof(MyGamesPage));
        Routing.RegisterRoute(nameof(LicenseListPage), typeof(LicenseListPage));
        Routing.RegisterRoute(nameof(LicenseDetailPage), typeof(LicenseDetailPage));

        TabBar tabBar = new();
        _gamesTab = CreateTab(AppResources.Play, "icon_games.png", gamesPage, nameof(GamesPage));
        _historyTab = CreateTab(AppResources.History, "icon_history.png", historyPage, nameof(HistoryPage));
        _settingsTab = CreateTab(AppResources.Settings, "icon_settings.png", settingsPage, nameof(AppSettingsPage));
        tabBar.Items.Add(_gamesTab);
        tabBar.Items.Add(_historyTab);
        _participantsTab = CreateTab(AppResources.Participants, "icon_groups.png", playerListPage, nameof(PlayerListPage));
        tabBar.Items.Add(_participantsTab);
        tabBar.Items.Add(_settingsTab);
        tabBar.CurrentItem = _gamesTab;

        Items.Add(tabBar);
        Navigated += OnShellNavigated;
    }

    public async Task NavigateToMainSectionAsync(AppMainSection section, bool remember = true)
    {
        section = AppMainSections.FromStoredValue((int)section) ?? AppMainSection.Play;
        string? openGameKey = section == AppMainSection.Play ? await _playOverview.GetSingleOpenGameKeyAsync() : null;
        Tab target = section switch
        {
            AppMainSection.Play => _gamesTab,
            AppMainSection.History => _historyTab,
            AppMainSection.Settings => _settingsTab,
            AppMainSection.Participants => _participantsTab,
            _ => _gamesTab
        };

        _gameSessionCaller = null;
        _gameSessionCallerSection = null;
        _navigationSettingsStore.Save(_navigationSettingsStore.Get() with { LastGameSessionId = null });
        _isProgrammaticMainNavigation = true;
        try
        {
            await GoToAsync($"//{target.Route}");
            if (openGameKey is not null)
                await PushGameGroupsAsync(openGameKey);
        }
        finally
        {
            _isProgrammaticMainNavigation = false;
        }

        if (remember)
        {
            RememberMainSection(section);
        }
    }


    public async Task NavigateToGroupsForGameAsync(string gameKey, bool remember = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);
        _gameSessionCaller = null;
        _gameSessionCallerSection = null;
        _isProgrammaticMainNavigation = true;
        try
        {
            await GoToAsync($"//{_gamesTab.Route}");
            await PushGameGroupsAsync(gameKey);
        }
        finally
        {
            _isProgrammaticMainNavigation = false;
        }
        if (remember) RememberMainSection(AppMainSection.Play);
    }

    private async Task PushGameGroupsAsync(string gameKey)
    {
        AppNavigationSettings current = _navigationSettingsStore.Get();
        _navigationSettingsStore.Save(current with { SelectedGameKey = gameKey, LastGameSessionId = null });
        await GoToAsync(nameof(GroupListPage), new ShellNavigationQueryParameters { [NavigationParameters.GameKey] = gameKey });
    }

    public void CaptureGameSessionCaller(bool useRoot = false)
    {
        _gameSessionCallerSection = CurrentItem.CurrentItem;
        var stack = _gameSessionCallerSection.Navigation.NavigationStack;
        _gameSessionCaller = useRoot ? stack.OfType<GroupListPage>().LastOrDefault() ?? stack[0] : stack[^1];
    }

    public async Task ReturnToGameSessionCallerAsync()
    {
        if (_isReturningToCaller)
            return;
        _isReturningToCaller = true;
        try
        {
            ShellSection section = CurrentItem.CurrentItem;
            var navigation = section.Navigation;
            Page caller = section == _gameSessionCallerSection && _gameSessionCaller is not null
                && navigation.NavigationStack.Contains(_gameSessionCaller)
                ? _gameSessionCaller : navigation.NavigationStack[0];
            // Remove intermediate session screens, then pop once to the retained caller.
            while (navigation.NavigationStack[^1] != caller
                && navigation.NavigationStack.Count > 1 && navigation.NavigationStack[^2] != caller)
                navigation.RemovePage(navigation.NavigationStack[^2]);
            if (navigation.NavigationStack[^1] != caller)
                await GoToAsync("..");
            // Leaving the session flow ends resume even when its caller is below the section root.
            AppNavigationSettings current = _navigationSettingsStore.Get();
            _navigationSettingsStore.Save(current with { LastGameSessionId = null });
            _gameSessionCaller = null;
            _gameSessionCallerSection = null;
        }
        finally
        {
            _isReturningToCaller = false;
        }
    }

    public void EnableNavigationMemory() => _isNavigationMemoryEnabled = true;

    // Native tab taps (including reselecting the active tab) use the same root routes.
    public async Task NavigateFromMainTabTapAsync(ShellSection tab)
    {
        if (_isMainTabTapInProgress)
            return;
        _isMainTabTapInProgress = true;
        try
        {
            await NavigateToMainSectionAsync(GetMainSection(tab));
        }
        finally
        {
            _isMainTabTapInProgress = false;
        }
    }

    private AppMainSection GetMainSection(ShellSection tab) => tab == _gamesTab ? AppMainSection.Play
        : tab == _historyTab ? AppMainSection.History
        : tab == _participantsTab ? AppMainSection.Participants
        : tab == _settingsTab ? AppMainSection.Settings
        : throw new InvalidOperationException("Unknown main tab.");

    public void RememberGameSession(Guid gameSessionId, string gameKey)
    {
        AppNavigationSettings current = _navigationSettingsStore.Get();
        _navigationSettingsStore.Save(current with
        {
            SelectedGameKey = gameKey,
            LastGameSessionId = gameSessionId
        });
    }

    private async void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        if (!_isNavigationMemoryEnabled || _isProgrammaticMainNavigation)
        {
            return;
        }

        AppMainSection section = GetMainSection(CurrentItem.CurrentItem);
        // A push inside a game must retain its resume target. Returning to a root clears it.
        RememberMainSection(section, CurrentItem.CurrentItem.Navigation.NavigationStack.Count <= 1);
    }

    private void RememberMainSection(AppMainSection section, bool clearGameSession = true)
    {
        AppNavigationSettings current = _navigationSettingsStore.Get();
        _navigationSettingsStore.Save(current with
        {
            LastMainSection = section,
            LastGameSessionId = clearGameSession ? null : current.LastGameSessionId
        });
    }

    private static Tab CreateTab(string title, string icon, Page content, string route)
    {
        Tab tab = new()
        {
            Title = title,
            Icon = icon,
            Route = route
        };
        tab.Items.Add(new ShellContent
        {
            Content = content
        });
        return tab;
    }
}

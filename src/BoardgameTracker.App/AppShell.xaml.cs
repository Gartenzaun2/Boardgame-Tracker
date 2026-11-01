using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.History;
using BoardgameTracker.App.Features.Settings.AppSettings;
using BoardgameTracker.App.Features.Settings.Licenses;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Settings.Navigation;
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
    private readonly Tab _groupsTab;
    private readonly Tab _historyTab;
    private readonly Tab _settingsTab;
    private bool _isProgrammaticMainNavigation;
    private bool _isNavigationMemoryEnabled;
    private bool _isMainTabTapInProgress;

    public AppShell(
        GroupListPage groupListPage,
        GamesPage gamesPage,
        HistoryPage historyPage,
        AppSettingsPage settingsPage,
        IAppNavigationSettingsStore navigationSettingsStore)
    {
        InitializeComponent();
        _navigationSettingsStore = navigationSettingsStore;

        Routing.RegisterRoute(nameof(CreateGroupPage), typeof(CreateGroupPage));
        Routing.RegisterRoute(nameof(EditGroupPage), typeof(EditGroupPage));
        Routing.RegisterRoute(nameof(PlayerListPage), typeof(PlayerListPage));
        Routing.RegisterRoute(nameof(EditPlayerPage), typeof(EditPlayerPage));
        Routing.RegisterRoute(nameof(LicenseListPage), typeof(LicenseListPage));
        Routing.RegisterRoute(nameof(LicenseDetailPage), typeof(LicenseDetailPage));

        TabBar tabBar = new();
        _gamesTab = CreateTab(AppResources.Games, "icon_games.png", gamesPage, nameof(GamesPage));
        _groupsTab = CreateTab(AppResources.Groups, "icon_groups.png", groupListPage, nameof(GroupListPage));
        _historyTab = CreateTab(AppResources.History, "icon_history.png", historyPage, nameof(HistoryPage));
        _settingsTab = CreateTab(AppResources.Settings, "icon_settings.png", settingsPage, nameof(AppSettingsPage));
        tabBar.Items.Add(_gamesTab);
        tabBar.Items.Add(_groupsTab);
        tabBar.Items.Add(_historyTab);
        tabBar.Items.Add(_settingsTab);
        tabBar.CurrentItem = _gamesTab;

        Items.Add(tabBar);
        Navigated += OnShellNavigated;
    }

    public async Task NavigateToMainSectionAsync(AppMainSection section, bool remember = true)
    {
        if (section == AppMainSection.Groups)
        {
            AppNavigationSettings settings = _navigationSettingsStore.Get();
            string? gameKey = settings.SelectedGameKey ?? settings.DefaultGameKey;
            if (string.IsNullOrWhiteSpace(gameKey))
                section = AppMainSection.Games;
            else if (string.IsNullOrWhiteSpace(settings.SelectedGameKey))
                _navigationSettingsStore.Save(settings with { SelectedGameKey = gameKey });
        }

        Tab target = section switch
        {
            AppMainSection.Games => _gamesTab,
            AppMainSection.Groups => _groupsTab,
            AppMainSection.History => _historyTab,
            AppMainSection.Settings => _settingsTab,
            _ => _gamesTab
        };

        _isProgrammaticMainNavigation = true;
        try
        {
            await GoToAsync($"//{target.Route}");
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

        AppNavigationSettings current = _navigationSettingsStore.Get();
        _navigationSettingsStore.Save(current with
        {
            SelectedGameKey = gameKey,
            LastMainSection = remember ? AppMainSection.Groups : current.LastMainSection,
            LastGameSessionId = null
        });

        await NavigateToMainSectionAsync(AppMainSection.Groups, remember);
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

    private AppMainSection GetMainSection(ShellSection tab) => tab == _gamesTab ? AppMainSection.Games
        : tab == _groupsTab ? AppMainSection.Groups
        : tab == _historyTab ? AppMainSection.History
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
        if (section == AppMainSection.Groups)
        {
            AppNavigationSettings current = _navigationSettingsStore.Get();
            if (string.IsNullOrWhiteSpace(current.SelectedGameKey)
                && string.IsNullOrWhiteSpace(current.DefaultGameKey))
            {
                await NavigateToMainSectionAsync(AppMainSection.Games);
                return;
            }

            if (string.IsNullOrWhiteSpace(current.SelectedGameKey)
                && !string.IsNullOrWhiteSpace(current.DefaultGameKey))
            {
                _navigationSettingsStore.Save(current with { SelectedGameKey = current.DefaultGameKey });
            }

        }

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

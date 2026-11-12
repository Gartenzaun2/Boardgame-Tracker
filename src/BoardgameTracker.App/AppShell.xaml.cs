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

        ShellSection previousSection = CurrentItem.CurrentItem;
        Page previousPage = previousSection.Navigation.NavigationStack[^1];
        _isProgrammaticMainNavigation = true;
        try
        {
            if (section == AppMainSection.Participants && _participantsTab.Items[0].Content is PlayerListPage players)
                players.SelectPlayers();
            string route = $"//{target.Route}";
            ShellNavigationQueryParameters parameters = new();
            if (openGameKey is not null)
            {
                route += $"/{nameof(GroupListPage)}";
                parameters[NavigationParameters.GameKey] = openGameKey;
            }
            // One Shell request builds the logical stack and presents only its final page.
            await GoToAsync(route, false, parameters);
        }
        catch
        {
            // Shell can change its stack before reporting an error. Keep the state of
            // an already reached root instead of restoring an obsolete session context.
            if (CurrentItem.CurrentItem == target && previousPage != target.Navigation.NavigationStack[^1]
                && (openGameKey is null ? target.Navigation.NavigationStack.Count == 1
                    : target.Navigation.NavigationStack[^1] is GroupListPage groups && groups.GameKey == openGameKey))
                RememberDestination();
            else
                ReconcileFailedNavigation(previousSection, previousPage);
            throw;
        }
        finally
        {
            _isProgrammaticMainNavigation = false;
        }

        RememberDestination();

        void RememberDestination()
        {
            ClearGameSessionCaller();
            AppNavigationSettings current = _navigationSettingsStore.Get();
            _navigationSettingsStore.Save(current with
            {
                LastGameSessionId = null,
                SelectedGameKey = openGameKey ?? current.SelectedGameKey,
                LastMainSection = remember ? section : current.LastMainSection
            });
        }
    }


    public async Task NavigateToGroupsForGameAsync(string gameKey, bool remember = true,
        string? destinationRoute = null, ShellNavigationQueryParameters? parameters = null, Guid? resumeGameSessionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);
        ShellSection previousSection = CurrentItem.CurrentItem;
        Page previousPage = previousSection.Navigation.NavigationStack[^1];
        _isProgrammaticMainNavigation = true;
        try
        {
            parameters ??= new();
            parameters[NavigationParameters.GameKey] = gameKey;
            string route = $"//{_gamesTab.Route}/{nameof(GroupListPage)}";
            if (destinationRoute is not null)
                route += $"/{destinationRoute}";
            await GoToAsync(route, false, parameters);
        }
        catch
        {
            if (CurrentItem.CurrentItem == _gamesTab && previousPage != _gamesTab.Navigation.NavigationStack[^1]
                && (destinationRoute is null
                    ? _gamesTab.Navigation.NavigationStack[^1] is GroupListPage groups && groups.GameKey == gameKey
                    : _gamesTab.Navigation.NavigationStack.Count >= 3
                        && _gamesTab.Navigation.NavigationStack[^2] is GroupListPage sessionGroups && sessionGroups.GameKey == gameKey))
                RememberDestination();
            else
                ReconcileFailedNavigation(previousSection, previousPage, preserveResumeTarget: resumeGameSessionId is not null);
            throw;
        }
        finally
        {
            _isProgrammaticMainNavigation = false;
        }
        RememberDestination();

        void RememberDestination()
        {
            ClearGameSessionCaller();
            if (destinationRoute is not null)
                CaptureGameSessionCaller(useRoot: true);
            AppNavigationSettings current = _navigationSettingsStore.Get();
            _navigationSettingsStore.Save(current with
            {
                SelectedGameKey = gameKey,
                LastGameSessionId = resumeGameSessionId,
                LastMainSection = remember ? AppMainSection.Play : current.LastMainSection
            });
        }
    }

    // Capture the actual entry page before a relative push, but publish the caller
    // and resume target only after Shell has reached the session destination.
    public async Task OpenGameSessionPageAsync(string route, ShellNavigationQueryParameters parameters,
        string gameKey, Guid? resumeGameSessionId)
    {
        ShellSection section = CurrentItem.CurrentItem;
        Page caller = section.Navigation.NavigationStack[^1];
        _isProgrammaticMainNavigation = true;
        try
        {
            await GoToAsync(route, false, parameters);
        }
        catch
        {
            // These handlers push one registered page. Inspect page identity rather
            // than CurrentState, which may not have been updated before the failure.
            if (CurrentItem.CurrentItem == section && section.Navigation.NavigationStack.Contains(caller)
                && section.Navigation.NavigationStack[^1] != caller)
                RememberDestination();
            else
                ReconcileFailedNavigation(section, caller);
            throw;
        }
        finally
        {
            _isProgrammaticMainNavigation = false;
        }
        RememberDestination();

        void RememberDestination()
        {
            _gameSessionCallerSection = CurrentItem.CurrentItem;
            var stack = _gameSessionCallerSection.Navigation.NavigationStack;
            _gameSessionCaller = _gameSessionCallerSection == section && stack.Contains(caller) ? caller : stack[0];
            AppNavigationSettings current = _navigationSettingsStore.Get();
            _navigationSettingsStore.Save(current with
            {
                SelectedGameKey = gameKey,
                LastGameSessionId = resumeGameSessionId,
                LastMainSection = _isNavigationMemoryEnabled ? GetMainSection(CurrentItem.CurrentItem) : current.LastMainSection
            });
        }
    }

    private void ClearGameSessionCaller()
    {
        _gameSessionCaller = null;
        _gameSessionCallerSection = null;
    }

    private void ReconcileFailedNavigation(ShellSection previousSection, Page previousPage, bool preserveResumeTarget = false)
    {
        ShellSection section = CurrentItem.CurrentItem;
        var stack = section.Navigation.NavigationStack;
        AppNavigationSettings current = _navigationSettingsStore.Get();
        if (section == previousSection && stack[^1] == previousPage
            && (previousPage is not GroupListPage visibleGroups || visibleGroups.GameKey == current.SelectedGameKey))
            return;

        // No UI rollback: record the section/game actually visible after a partial
        // transition. A startup resume target remains pending until its retry.
        GroupListPage? groups = stack.OfType<GroupListPage>().LastOrDefault();
        _navigationSettingsStore.Save(current with
        {
            SelectedGameKey = !string.IsNullOrWhiteSpace(groups?.GameKey) ? groups.GameKey : current.SelectedGameKey,
            LastMainSection = _isNavigationMemoryEnabled ? GetMainSection(section) : current.LastMainSection,
            LastGameSessionId = preserveResumeTarget ? current.LastGameSessionId : null
        });
        ClearGameSessionCaller();
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
        ShellSection section = CurrentItem.CurrentItem;
        var navigation = section.Navigation;
        Page previousPage = navigation.NavigationStack[^1];
        Page caller = section == _gameSessionCallerSection && _gameSessionCaller is not null
            && navigation.NavigationStack.Contains(_gameSessionCaller)
            ? _gameSessionCaller : navigation.NavigationStack[0];
        _isProgrammaticMainNavigation = true;
        try
        {
            // Remove intermediate session screens, then pop once to the retained caller.
            while (navigation.NavigationStack[^1] != caller
                && navigation.NavigationStack.Count > 1 && navigation.NavigationStack[^2] != caller)
                navigation.RemovePage(navigation.NavigationStack[^2]);
            if (navigation.NavigationStack[^1] != caller)
                await GoToAsync("..", false);
            // Leaving the session flow ends resume even when its caller is below the section root.
            RememberReturn();
        }
        catch
        {
            if (CurrentItem.CurrentItem == section && navigation.NavigationStack[^1] == caller)
                RememberReturn();
            else
            {
                ReconcileFailedNavigation(section, previousPage);
                if (CurrentItem.CurrentItem == section && navigation.NavigationStack.Contains(caller))
                {
                    _gameSessionCallerSection = section;
                    _gameSessionCaller = caller;
                }
            }
            throw;
        }
        finally
        {
            _isReturningToCaller = false;
            _isProgrammaticMainNavigation = false;
        }

        void RememberReturn()
        {
            AppNavigationSettings current = _navigationSettingsStore.Get();
            _navigationSettingsStore.Save(current with { LastGameSessionId = null });
            ClearGameSessionCaller();
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

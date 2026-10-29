using BoardgameTracker.App.Games.ArkNova.Features.SpecialCards;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running;
using BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring;
using BoardgameTracker.App.Games.ArkNova.Features.Settings;
using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.History;
using BoardgameTracker.App.Features.Settings.AppSettings;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Application.Settings.Navigation;

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
        Routing.RegisterRoute(nameof(ArkNovaPreparationPage), typeof(ArkNovaPreparationPage));
        Routing.RegisterRoute(nameof(ArkNovaRunningPage), typeof(ArkNovaRunningPage));
        Routing.RegisterRoute(nameof(ArkNovaScoringPage), typeof(ArkNovaScoringPage));
        Routing.RegisterRoute(nameof(ArkNovaPlayerScoringPage), typeof(ArkNovaPlayerScoringPage));
        Routing.RegisterRoute(nameof(ArkNovaTieResolutionPage), typeof(ArkNovaTieResolutionPage));
        Routing.RegisterRoute(nameof(ArkNovaResultPage), typeof(ArkNovaResultPage));
        Routing.RegisterRoute(nameof(ArkNovaSpecialCardsPage), typeof(ArkNovaSpecialCardsPage));
        Routing.RegisterRoute(nameof(ArkNovaSpecialCardEditorPage), typeof(ArkNovaSpecialCardEditorPage));
        Routing.RegisterRoute(nameof(ArkNovaSettingsPage), typeof(ArkNovaSettingsPage));

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
        Tab target = section switch
        {
            AppMainSection.Games => _gamesTab,
            AppMainSection.Groups => _groupsTab,
            AppMainSection.History => _historyTab,
            AppMainSection.Settings => _settingsTab,
            _ => _gamesTab
        };

        _isProgrammaticMainNavigation = !remember;
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

        string location = CurrentState.Location.OriginalString;
        if (location.EndsWith(nameof(GamesPage), StringComparison.Ordinal))
        {
            RememberMainSection(AppMainSection.Games);
        }
        else if (location.EndsWith(nameof(GroupListPage), StringComparison.Ordinal))
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

            RememberMainSection(AppMainSection.Groups);
        }
        else if (location.EndsWith(nameof(HistoryPage), StringComparison.Ordinal))
        {
            RememberMainSection(AppMainSection.History);
        }
        else if (location.EndsWith(nameof(AppSettingsPage), StringComparison.Ordinal))
        {
            RememberMainSection(AppMainSection.Settings);
        }
    }

    private void RememberMainSection(AppMainSection section)
    {
        AppNavigationSettings current = _navigationSettingsStore.Get();
        _navigationSettingsStore.Save(current with
        {
            LastMainSection = section,
            LastGameSessionId = null
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

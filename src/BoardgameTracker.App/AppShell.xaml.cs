using BoardgameTracker.App.Features.GameSessions.Preparation;
using BoardgameTracker.App.Features.GameSessions.Result;
using BoardgameTracker.App.Features.GameSessions.Running;
using BoardgameTracker.App.Features.GameSessions.Scoring;
using BoardgameTracker.App.Features.Games.ArkNova.Settings;
using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.History;
using BoardgameTracker.App.Features.Settings.AppSettings;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App;

/// <summary>
/// Defines the MAUI Shell root and registers feature routes; it does not contain domain or persistence logic.
/// </summary>
public partial class AppShell : Shell
{
    private bool _isResettingGroupTab;

    public AppShell(
        GroupListPage groupListPage,
        GamesPage gamesPage,
        HistoryPage historyPage,
        AppSettingsPage settingsPage)
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(CreateGroupPage), typeof(CreateGroupPage));
        Routing.RegisterRoute(nameof(EditGroupPage), typeof(EditGroupPage));
        Routing.RegisterRoute(nameof(ArkNovaPreparationPage), typeof(ArkNovaPreparationPage));
        Routing.RegisterRoute(nameof(ArkNovaRunningPage), typeof(ArkNovaRunningPage));
        Routing.RegisterRoute(nameof(ArkNovaScoringPage), typeof(ArkNovaScoringPage));
        Routing.RegisterRoute(nameof(ArkNovaPlayerScoringPage), typeof(ArkNovaPlayerScoringPage));
        Routing.RegisterRoute(nameof(ArkNovaTieResolutionPage), typeof(ArkNovaTieResolutionPage));
        Routing.RegisterRoute(nameof(ArkNovaResultPage), typeof(ArkNovaResultPage));
        Routing.RegisterRoute(nameof(ArkNovaSettingsPage), typeof(ArkNovaSettingsPage));

        TabBar tabBar = new();
        Tab gamesTab = CreateTab(AppResources.Games, "icon_games.png", gamesPage, nameof(GamesPage));
        Tab groupsTab = CreateTab(AppResources.Groups, "icon_groups.png", groupListPage, nameof(GroupListPage));
        Tab historyTab = CreateTab(AppResources.History, "icon_history.png", historyPage, nameof(HistoryPage));
        Tab settingsTab = CreateTab(AppResources.Settings, "icon_settings.png", settingsPage, nameof(AppSettingsPage));
        tabBar.Items.Add(gamesTab);
        tabBar.Items.Add(groupsTab);
        tabBar.Items.Add(historyTab);
        tabBar.Items.Add(settingsTab);
        tabBar.CurrentItem = groupsTab;

        Items.Add(tabBar);
        Navigating += OnShellNavigating;
    }

    private async void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (_isResettingGroupTab
            || e.Source is not (ShellNavigationSource.ShellSectionChanged or ShellNavigationSource.ShellContentChanged)
            || !e.Target.Location.OriginalString.Contains(nameof(GroupListPage), StringComparison.Ordinal))
        {
            return;
        }

        string current = CurrentState.Location.OriginalString;
        if (current.EndsWith(nameof(GroupListPage), StringComparison.Ordinal))
        {
            return;
        }

        var deferral = e.GetDeferral();
        e.Cancel();
        _isResettingGroupTab = true;
        try
        {
            await GoToAsync($"//{nameof(GroupListPage)}");
        }
        finally
        {
            _isResettingGroupTab = false;
            deferral.Complete();
        }
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

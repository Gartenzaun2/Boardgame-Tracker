using BoardgameTracker.App.Features.GameSessions.Preparation;
using BoardgameTracker.App.Features.GameSessions.Result;
using BoardgameTracker.App.Features.GameSessions.Running;
using BoardgameTracker.App.Features.GameSessions.Scoring;
using BoardgameTracker.App.Features.Games.ArkNova.Settings;
using BoardgameTracker.App.Features.Games.ListGames;
using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.EditGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Features.Settings.AppSettings;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App;

/// <summary>
/// Defines the MAUI Shell root and registers feature routes; it does not contain domain or persistence logic.
/// </summary>
public partial class AppShell : Shell
{
    public AppShell(
        GroupListPage groupListPage,
        GamesPage gamesPage,
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
        tabBar.Items.Add(CreateTab(AppResources.Groups, "icon_groups.png", groupListPage, nameof(GroupListPage)));
        tabBar.Items.Add(CreateTab(AppResources.Games, "icon_games.png", gamesPage, nameof(GamesPage)));
        tabBar.Items.Add(CreateTab(AppResources.Settings, "icon_settings.png", settingsPage, nameof(AppSettingsPage)));

        Items.Add(tabBar);
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

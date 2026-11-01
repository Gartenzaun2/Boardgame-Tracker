// These doubles record Shell calls without starting a platform UI. The linked
// AppShell and game-session handler remain the production implementations.
global using Microsoft.Maui.Controls;

namespace Microsoft.Maui.Controls
{
    public class Page;
    public sealed class ShellNavigationQueryParameters : Dictionary<string, object>;
    public sealed class ShellNavigatedEventArgs : EventArgs;
    public sealed class ShellNavigation
    {
        public List<Page> NavigationStackPages { get; } = [new Page()];
        public IReadOnlyList<Page> NavigationStack => NavigationStackPages;
    }
    public class ShellSection
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Route { get; set; } = "";
        public List<ShellContent> Items { get; } = [];
        public ShellNavigation Navigation { get; } = new();
    }
    public sealed class Tab : ShellSection;
    public sealed class ShellContent { public Page? Content { get; set; } }
    public sealed class TabBar
    {
        public List<Tab> Items { get; } = [];
        public Tab CurrentItem { get; set; } = null!;
    }
    public static class Routing
    {
        public static void RegisterRoute(string route, Type page) { }
    }
    public class Shell
    {
        public static Shell Current { get; set; } = null!;
        public List<TabBar> Items { get; } = [];
        public TabBar CurrentItem => Items[0];
        public List<(string Route, ShellNavigationQueryParameters Parameters)> Calls { get; } = [];
        public event EventHandler<ShellNavigatedEventArgs>? Navigated;
        public Task GoToAsync(string route, ShellNavigationQueryParameters? parameters = null)
        {
            Calls.Add((route, parameters ?? []));
            if (route.StartsWith("//", StringComparison.Ordinal))
            {
                CurrentItem.CurrentItem = CurrentItem.Items.Single(tab => tab.Route == route[2..]);
                CurrentItem.CurrentItem.Navigation.NavigationStackPages.RemoveRange(1,
                    CurrentItem.CurrentItem.Navigation.NavigationStack.Count - 1);
            }
            else
                CurrentItem.CurrentItem.Navigation.NavigationStackPages.Add(new Page());
            Navigated?.Invoke(this, new());
            return Task.CompletedTask;
        }
        public void NotifyNavigated() => Navigated?.Invoke(this, new());
    }
}

namespace BoardgameTracker.App { public partial class AppShell { private void InitializeComponent() { } } }
namespace BoardgameTracker.App.Features.Games.ListGames { public sealed class GamesPage : Page; }
namespace BoardgameTracker.App.Features.Groups.CreateGroup { public sealed class CreateGroupPage : Page; }
namespace BoardgameTracker.App.Features.Groups.EditGroup { public sealed class EditGroupPage : Page; }
namespace BoardgameTracker.App.Features.Groups.ListGroups { public sealed class GroupListPage : Page; }
namespace BoardgameTracker.App.Features.History { public sealed class HistoryPage : Page; }
namespace BoardgameTracker.App.Features.Settings.AppSettings { public sealed class AppSettingsPage : Page; }
namespace BoardgameTracker.App.Features.Settings.Licenses
{
    public sealed class LicenseListPage : Page;
    public sealed class LicenseDetailPage : Page;
}
namespace BoardgameTracker.App.Features.Players.ListPlayers { public sealed class PlayerListPage : Page; }
namespace BoardgameTracker.App.Features.Players.EditPlayer { public sealed class EditPlayerPage : Page; }
namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation { public sealed class ArkNovaPreparationPage : Page; }
namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running { public sealed class ArkNovaRunningPage : Page; }
namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring { public sealed class ArkNovaScoringPage : Page; }
namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result { public sealed class ArkNovaResultPage : Page; }

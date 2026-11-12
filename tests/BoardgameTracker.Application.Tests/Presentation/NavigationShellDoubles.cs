// These doubles record Shell calls without starting a platform UI. The linked
// AppShell and game-session handler remain the production implementations.
global using Microsoft.Maui.Controls;

namespace Microsoft.Maui.Controls
{
    public class Page;
    public sealed class ShellNavigationQueryParameters : Dictionary<string, object>;
    public sealed class ShellNavigatedEventArgs : EventArgs;
    public enum NavigationFailurePoint { Before, AfterRoot, AfterGroups, After }
    public sealed class ShellNavigation
    {
        public List<Page> NavigationStackPages { get; } = [new Page()];
        public IReadOnlyList<Page> NavigationStack => NavigationStackPages;
        public void RemovePage(Page page) => NavigationStackPages.Remove(page);
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
        public NavigationFailurePoint? FailNextNavigation { get; set; }
        public Exception NavigationFailure { get; } = new InvalidOperationException("Shell navigation failed.");
        // Parallel tests each represent an independent UI; their current Shell must
        // follow the async test context rather than leak into another test.
        private static readonly AsyncLocal<Shell?> CurrentShell = new();
        public static Shell Current { get => CurrentShell.Value!; set => CurrentShell.Value = value; }
        public List<TabBar> Items { get; } = [];
        public TabBar CurrentItem => Items[0];
        public List<(string Route, ShellNavigationQueryParameters Parameters)> Calls { get; } = [];
        public event EventHandler<ShellNavigatedEventArgs>? Navigated;
        public List<bool> Animations { get; } = [];
        public Task GoToAsync(string route, ShellNavigationQueryParameters? parameters = null) => GoToAsync(route, true, parameters);
        public Task GoToAsync(string route, bool animate, ShellNavigationQueryParameters? parameters = null)
        {
            Calls.Add((route, parameters ?? []));
            Animations.Add(animate);
            NavigationFailurePoint? failure = FailNextNavigation;
            FailNextNavigation = null;
            if (failure == NavigationFailurePoint.Before)
                return Task.FromException(NavigationFailure);
            if (route.StartsWith("//", StringComparison.Ordinal))
            {
                string[] parts = route[2..].Split('/');
                CurrentItem.CurrentItem = CurrentItem.Items.Single(tab => tab.Route == parts[0]);
                CurrentItem.CurrentItem.Navigation.NavigationStackPages.RemoveRange(1,
                    CurrentItem.CurrentItem.Navigation.NavigationStack.Count - 1);
                if (failure == NavigationFailurePoint.AfterRoot)
                    return Task.FromException(NavigationFailure);
                foreach (string part in parts.Skip(1))
                {
                    AddPage(part, parameters);
                    if (failure == NavigationFailurePoint.AfterGroups && part == "GroupListPage")
                        return Task.FromException(NavigationFailure);
                }
            }
            else if (route == ".." || route == "../..")
            {
                int count = route == ".." ? 1 : 2;
                var stack = CurrentItem.CurrentItem.Navigation.NavigationStackPages;
                stack.RemoveRange(stack.Count - count, count);
            }
            else
                AddPage(route, parameters);
            Navigated?.Invoke(this, new());
            if (failure == NavigationFailurePoint.After)
                return Task.FromException(NavigationFailure);
            return Task.CompletedTask;
        }
        private void AddPage(string route, ShellNavigationQueryParameters? parameters)
        {
            Page page = route == "GroupListPage" ? new BoardgameTracker.App.Features.Groups.ListGroups.GroupListPage
            {
                GameKey = parameters?.GetValueOrDefault(BoardgameTracker.App.Navigation.NavigationParameters.GameKey) as string ?? ""
            } : new Page();
            CurrentItem.CurrentItem.Navigation.NavigationStackPages.Add(page);
        }
        public void NotifyNavigated() => Navigated?.Invoke(this, new());
    }
}

namespace BoardgameTracker.App { public partial class AppShell { private void InitializeComponent() { } } }
namespace BoardgameTracker.App.Features.Games.ListGames { public sealed class GamesPage : Page; }
namespace BoardgameTracker.App.Features.Groups.CreateGroup { public sealed class CreateGroupPage : Page; public sealed class DuplicateGroupPage : Page; }
namespace BoardgameTracker.App.Features.Groups.EditGroup { public sealed class EditGroupPage : Page; }
namespace BoardgameTracker.App.Features.Groups.ListGroups { public sealed class GroupListPage : Page { public string GameKey { get; set; } = ""; } }
namespace BoardgameTracker.App.Features.History { public sealed class HistoryPage : Page; }
namespace BoardgameTracker.App.Features.Settings.AppSettings { public sealed class AppSettingsPage : Page; }
namespace BoardgameTracker.App.Features.Settings.Licenses
{
    public sealed class LicenseListPage : Page;
    public sealed class LicenseDetailPage : Page;
}
namespace BoardgameTracker.App.Features.Players.ListPlayers { public sealed class PlayerListPage : Page { public void SelectPlayers() { } } }
namespace BoardgameTracker.App.Features.Players.EditPlayer { public sealed class EditPlayerPage : Page; }
namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Preparation { public sealed class ArkNovaPreparationPage : Page; }
namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Running { public partial class ArkNovaRunningPage : Page; }
namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Scoring { public partial class ArkNovaScoringPage : Page; }
namespace BoardgameTracker.App.Games.ArkNova.Features.GameSessions.Result { public partial class ArkNovaResultPage : Page
    {
        public TestHeader PageHeader { get; } = new();
        public TestButton EditButton { get; } = new();
        public TestButton DoneButton { get; } = new();
    }
    public sealed class TestHeader { public bool ShowBack { get; set; } }
    public sealed class TestButton { public bool IsVisible { get; set; } } }

namespace BoardgameTracker.App.Features.Settings.MyGames { public sealed class MyGamesPage : Page; }

namespace BoardgameTracker.App.Games.ArkNova.Features.Settings { public sealed class ArkNovaSettingsPage : Page; }

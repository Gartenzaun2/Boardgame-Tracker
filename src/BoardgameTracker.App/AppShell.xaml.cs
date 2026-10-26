using BoardgameTracker.App.Features.Groups;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App;

public partial class AppShell : Shell
{
    public AppShell(GroupListPage groupListPage)
    {
        InitializeComponent();

        Items.Add(new ShellContent
        {
            Title = AppResources.Groups,
            Content = groupListPage,
            Route = nameof(GroupListPage)
        });
    }
}

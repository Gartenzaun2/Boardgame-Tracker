using BoardgameTracker.App.Features.Groups.CreateGroup;
using BoardgameTracker.App.Features.Groups.ListGroups;
using BoardgameTracker.App.Resources.Localization;

namespace BoardgameTracker.App;

public partial class AppShell : Shell
{
    public AppShell(GroupListPage groupListPage)
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(CreateGroupPage), typeof(CreateGroupPage));

        Items.Add(new ShellContent
        {
            Title = AppResources.Groups,
            Content = groupListPage,
            Route = nameof(GroupListPage)
        });
    }
}

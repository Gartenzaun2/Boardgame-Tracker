using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Features.Groups;

public partial class GroupListViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Heading { get; set; } = "Boardgame Tracker";
}

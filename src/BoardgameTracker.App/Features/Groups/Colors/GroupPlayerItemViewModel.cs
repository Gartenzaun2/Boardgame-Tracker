using BoardgameTracker.App.Presentation.PlayerColors;
using BoardgameTracker.App.Resources.Localization;
using BoardgameTracker.Core.Players;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BoardgameTracker.App.Features.Groups.Colors;

public partial class GroupPlayerItemViewModel(Guid? playerId, string name, bool isGuest, PlayerColor? defaultColor = null) : ObservableObject
{
    public Guid? PlayerId { get; } = playerId;
    public string Name { get; } = name;
    public bool IsGuest { get; } = isGuest;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ColorName))]
    public partial PlayerColor? DefaultColor { get; set; } = defaultColor;
    public string ColorName => DefaultColor is { } color ? PlayerColorPresentation.GetName(color) : AppResources.NoPlayerColor;
}

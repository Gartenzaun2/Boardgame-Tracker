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
    [NotifyPropertyChangedFor(nameof(HasColor))]
    [NotifyPropertyChangedFor(nameof(CardBackground))]
    [NotifyPropertyChangedFor(nameof(CardTextColor))]
    public partial PlayerColor? DefaultColor { get; set; } = defaultColor;
    public bool HasColor => DefaultColor.HasValue;
    public Microsoft.Maui.Graphics.Color CardBackground => PlayerColorPresentation.GetBackground(DefaultColor ?? PlayerColor.Gray);
    public Microsoft.Maui.Graphics.Color CardTextColor => PlayerColorPresentation.GetText(DefaultColor ?? PlayerColor.Gray);
    public string ColorName => DefaultColor is { } color ? PlayerColorPresentation.GetName(color) : AppResources.NoPlayerColor;
}

using BoardgameTracker.App.Presentation.PlayerColors;
using BoardgameTracker.Core.Players;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiColor = Microsoft.Maui.Graphics.Color;

namespace BoardgameTracker.App.Games.ManualGames.Features.GameSessions;

public partial class ManualParticipantViewModel(Guid playerId, string name) : ObservableObject
{
    public Guid PlayerId { get; } = playerId;
    public string Name { get; } = name;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PositionText))]
    [NotifyPropertyChangedFor(nameof(HasOrder))]
    public partial int? Position { get; set; }
    public string PositionText => Position?.ToString() ?? string.Empty;
    public bool HasOrder => Position.HasValue;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasColor))]
    [NotifyPropertyChangedFor(nameof(ColorBrush))]
    [NotifyPropertyChangedFor(nameof(PlayerTextColor))]
    [NotifyPropertyChangedFor(nameof(ColorName))]
    public partial PlayerColor? Color { get; set; }
    public bool HasColor => Color.HasValue;
    public MauiColor ColorBrush => Color is PlayerColor color ? PlayerColorPresentation.GetBackground(color) : MauiColor.FromArgb("#00000000");
    public MauiColor PlayerTextColor => Color is PlayerColor color ? PlayerColorPresentation.GetText(color) : MauiColor.FromArgb("#00000000");
    public string ColorName => Color is PlayerColor color ? PlayerColorPresentation.GetName(color) : string.Empty;
    [ObservableProperty] public partial bool CanMoveUp { get; set; }
    [ObservableProperty] public partial bool CanMoveDown { get; set; }
    public bool CanChooseColor { get; init; }
}

public sealed record ManualColorOption(PlayerColor Color, string Name, MauiColor Background, MauiColor Text, bool IsSelected);

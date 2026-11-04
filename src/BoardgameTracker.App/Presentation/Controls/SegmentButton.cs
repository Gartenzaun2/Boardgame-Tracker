namespace BoardgameTracker.App.Presentation.Controls;

/// <summary>Selection state for the shared segment style; navigation stays with the page.</summary>
public sealed class SegmentButton : Button
{
    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
        nameof(IsSelected), typeof(bool), typeof(SegmentButton), false);
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}

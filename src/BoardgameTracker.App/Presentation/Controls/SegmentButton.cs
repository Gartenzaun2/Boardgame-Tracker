namespace BoardgameTracker.App.Presentation.Controls;

/// <summary>Selection state for the shared segment style; navigation stays with the page.</summary>
public sealed class SegmentButton : Button
{
    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
        nameof(IsSelected), typeof(bool), typeof(SegmentButton), false,
        propertyChanged: (bindable, _, _) => ((SegmentButton)bindable).ChangeVisualState());
    // The same path handles selection, inherited IsEnabled changes and native button state updates.
    // Each target state owns all its colors; no trigger values must be restored afterwards.
    protected override void ChangeVisualState()
    {
        string state = !IsEnabled ? VisualStateManager.CommonStates.Disabled
            : IsSelected ? VisualStateManager.CommonStates.Selected
            : VisualStateManager.CommonStates.Normal;
        VisualStateManager.GoToState(this, state);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}

namespace BoardgameTracker.App.Presentation.Controls;

/// <summary>A bounded native wheel. A displayed suggestion does not change Value.</summary>
public sealed class NumberWheel : View
{
    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(nameof(Minimum), typeof(int), typeof(NumberWheel), 0);
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(nameof(Maximum), typeof(int), typeof(NumberWheel), 100);
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(int?), typeof(NumberWheel), null, BindingMode.TwoWay);
    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(NumberWheel), Colors.Black);

    public int Minimum { get => (int)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public int Maximum { get => (int)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public int? Value { get => (int?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public Color TextColor { get => (Color)GetValue(TextColorProperty); set => SetValue(TextColorProperty, value); }
}

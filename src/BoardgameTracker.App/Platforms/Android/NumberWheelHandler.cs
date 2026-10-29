using Android.Util;
using Android.Views;
using Android.Widget;
using BoardgameTracker.App.Presentation.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace BoardgameTracker.App.Platforms.Android;

public sealed class NumberWheelHandler() : ViewHandler<NumberWheel, NumberPicker>(Mapper)
{
    private static readonly IPropertyMapper<NumberWheel, NumberWheelHandler> Mapper =
        new PropertyMapper<NumberWheel, NumberWheelHandler>(ViewMapper)
        {
            [nameof(NumberWheel.Minimum)] = UpdateWheel,
            [nameof(NumberWheel.Maximum)] = UpdateWheel,
            [nameof(NumberWheel.Value)] = UpdateWheel,
            [nameof(NumberWheel.TextColor)] = UpdateWheel
        };
    private bool _updating;

    protected override NumberPicker CreatePlatformView() => new(Context)
    {
        WrapSelectorWheel = false,
        DescendantFocusability = DescendantFocusability.BlockDescendants
    };

    protected override void ConnectHandler(NumberPicker platformView)
    {
        base.ConnectHandler(platformView);
        platformView.ValueChanged += OnValueChanged;
    }

    protected override void DisconnectHandler(NumberPicker platformView)
    {
        platformView.ValueChanged -= OnValueChanged;
        base.DisconnectHandler(platformView);
    }

    private static void UpdateWheel(NumberWheelHandler handler, NumberWheel wheel)
    {
        handler._updating = true;
        try
        {
            NumberPicker picker = handler.PlatformView;
            picker.MinValue = wheel.Minimum;
            picker.MaxValue = wheel.Maximum;
            picker.WrapSelectorWheel = false;
            picker.Value = new NumberWheelRange(wheel.Minimum, wheel.Maximum).DisplayedValue(wheel.Value);
            picker.SetTextColor(wheel.TextColor.ToPlatform());
            picker.TextSize = TypedValue.ApplyDimension(ComplexUnitType.Sp, 24, handler.Context.Resources!.DisplayMetrics);
        }
        finally
        {
            handler._updating = false;
        }
    }

    private void OnValueChanged(object? sender, NumberPicker.ValueChangeEventArgs e)
    {
        if (!_updating)
            VirtualView.Value = e.NewVal;
    }
}

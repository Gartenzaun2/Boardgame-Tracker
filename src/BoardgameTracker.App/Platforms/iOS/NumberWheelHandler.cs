using BoardgameTracker.App.Presentation.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace BoardgameTracker.App.Platforms.iOS;

public sealed class NumberWheelHandler() : ViewHandler<NumberWheel, UIPickerView>(Mapper) {
    private static readonly IPropertyMapper<NumberWheel, NumberWheelHandler> Mapper =
        new PropertyMapper<NumberWheel, NumberWheelHandler>(ViewMapper) {
            [nameof(NumberWheel.Minimum)] = UpdateWheel,
            [nameof(NumberWheel.Maximum)] = UpdateWheel,
            [nameof(NumberWheel.Value)] = UpdateWheel,
            [nameof(NumberWheel.TextColor)] = UpdateWheel
        };
    private WheelModel? _model;

    protected override UIPickerView CreatePlatformView()
        => new();

    /// <summary>MAUI attaches the native picker; retain its model for the lifetime of this connection.</summary>
    protected override void ConnectHandler(UIPickerView platformView) {
        base.ConnectHandler(platformView);
        _model = new WheelModel(this);
        platformView.Model = _model;
    }

    /// <summary>MAUI detaches the native picker; disconnect and dispose the retained model before releasing the view.</summary>
    protected override void DisconnectHandler(UIPickerView platformView) {
        platformView.Model = null;
        _model?.Dispose();
        _model = null;
        base.DisconnectHandler(platformView);
    }

    private static void UpdateWheel(NumberWheelHandler handler, NumberWheel wheel) {
        int displayed = new NumberWheelRange(wheel.Minimum, wheel.Maximum).DisplayedValue(wheel.Value);
        handler.PlatformView.ReloadAllComponents();
        handler.PlatformView.Select(displayed - wheel.Minimum, 0, false);
    }

    private sealed class WheelModel(NumberWheelHandler handler) : UIPickerViewModel {
        public override nint GetComponentCount(UIPickerView pickerView)
            => 1;
        public override nint GetRowsInComponent(UIPickerView pickerView, nint component)
            => handler.VirtualView.Maximum - handler.VirtualView.Minimum + 1;
        public override nfloat GetRowHeight(UIPickerView pickerView, nint component)
            => 44;
        /// <summary>UIKit requests a row view; reuse its label and apply the current theme and bounded wheel value.</summary>
        public override UIView GetView(UIPickerView pickerView, nint row, nint component, UIView? view) {
            UILabel label = view as UILabel ?? new UILabel();
            label.Text = (handler.VirtualView.Minimum + (int)row).ToString();
            label.TextAlignment = UITextAlignment.Center;
            label.Font = UIFont.SystemFontOfSize(24);
            label.TextColor = handler.VirtualView.TextColor.ToPlatform();
            return label;
        }
        public override void Selected(UIPickerView pickerView, nint row, nint component)
            => handler.VirtualView.Value = handler.VirtualView.Minimum + (int)row;
    }
}

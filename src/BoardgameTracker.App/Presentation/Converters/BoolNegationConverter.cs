using System.Globalization;
using Microsoft.Maui.Controls;

namespace BoardgameTracker.App.Presentation.Converters;

/// <summary>Inverts boolean bindings in either direction; unsupported values leave the target unchanged.</summary>
public sealed class BoolNegationConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool flag ? !flag : BindableProperty.UnsetValue;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool flag ? !flag : BindableProperty.UnsetValue;
}

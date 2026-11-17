using System.Globalization;
using Microsoft.Maui.Controls;

namespace BoardgameTracker.App.Presentation.Converters;

/// <summary>Compares a bound enum with a typed enum parameter without interpreting names or numeric values.</summary>
public sealed class EnumEqualsConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Enum enumValue && parameter is Enum && Enum.IsDefined(enumValue.GetType(), enumValue) && enumValue.Equals(parameter);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => BindableProperty.UnsetValue;
}

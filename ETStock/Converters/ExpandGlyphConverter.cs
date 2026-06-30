using System.Globalization;
using Avalonia.Data.Converters;

namespace ETStock.Converters;

public sealed class ExpandGlyphConverter : IValueConverter
{
    public static readonly ExpandGlyphConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "▼" : "▶";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

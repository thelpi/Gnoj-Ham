using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Gnoj_Ham_View.Converters;

/// <summary>
/// Shows an element when <c>True</c>, and hides it - but keeps its room in the layout - otherwise.
/// (The framework's own converter collapses it, which frees that room.)
/// </summary>
internal sealed class BooleanToHiddenConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Hidden;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

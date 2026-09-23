using System.Globalization;
using System.Windows.Data;

namespace Brezee.App.Shell;

// True for a non-zero number, e.g. to enable a menu only when its list has items.
public sealed class NonZeroConverter : IValueConverter
{
    public static NonZeroConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is int count && count != 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

using System.Globalization;
using System.Windows.Data;

namespace Brezee.App.Commands;

// True when the value is an AppCommand. Used by the MenuItem style to pick up command text and shortcuts.
public sealed class IsAppCommandConverter : IValueConverter
{
    public static IsAppCommandConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is AppCommand;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

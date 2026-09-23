using System.Globalization;
using System.Windows.Data;

namespace Brezee.App.Shell;

// DockingManager.ActiveContent is also set when a tool panel gains focus. This converter lets only
// documents flow back into ShellViewModel.ActiveDocument, so it always points at a document tab.
public sealed class ActiveDocumentConverter : IValueConverter
{
    public static ActiveDocumentConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DocumentViewModel ? value : Binding.DoNothing;
}

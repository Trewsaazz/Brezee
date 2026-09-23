using System.Windows;
using System.Windows.Controls;

namespace Brezee.App.Shell;

// Picks the AvalonDock container style for a pane based on its view model type.
public sealed class PaneStyleSelector : StyleSelector
{
    public Style? ToolStyle { get; set; }

    public Style? DocumentStyle { get; set; }

    public override Style? SelectStyle(object item, DependencyObject container) => item switch
    {
        ToolViewModel => ToolStyle,
        DocumentViewModel => DocumentStyle,
        _ => base.SelectStyle(item, container),
    };
}

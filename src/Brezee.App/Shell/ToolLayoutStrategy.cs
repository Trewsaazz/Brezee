using AvalonDock.Layout;

namespace Brezee.App.Shell;

// Places each tool panel into the pane named after its preferred location
// (see the layout skeleton in MainWindow.xaml).
public sealed class ToolLayoutStrategy : ILayoutUpdateStrategy
{
    public const string LeftPaneName = "LeftToolPane";
    public const string BottomPaneName = "BottomToolPane";

    public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
    {
        // Respect an explicit destination, e.g. when the user drags a panel somewhere.
        if (destinationContainer is LayoutAnchorablePane)
            return false;

        if (anchorableToShow.Content is not ToolViewModel tool)
            return false;

        var paneName = tool.Location switch
        {
            ToolLocation.Left => LeftPaneName,
            ToolLocation.Bottom => BottomPaneName,
            _ => throw new ArgumentOutOfRangeException(nameof(anchorableToShow), tool.Location, "Unknown tool location."),
        };

        var pane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(p => p.Name == paneName);
        if (pane is null)
            return false;

        pane.Children.Add(anchorableToShow);
        return true;
    }

    public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
    {
    }

    public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer) => false;

    public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
    {
    }
}

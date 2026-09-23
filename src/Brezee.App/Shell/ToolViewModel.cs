namespace Brezee.App.Shell;

public enum ToolLocation
{
    Left,
    Bottom,
}

// A tool panel (explorer, output, ...) docked around the document area.
public abstract class ToolViewModel : PaneViewModel
{
    protected ToolViewModel(string contentId, string title, ToolLocation location)
        : base(contentId, title)
    {
        Location = location;
    }

    // Where the panel is docked on first launch.
    public ToolLocation Location { get; }
}

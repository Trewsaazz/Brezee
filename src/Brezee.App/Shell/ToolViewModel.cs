using CommunityToolkit.Mvvm.ComponentModel;

namespace Brezee.App.Shell;

public enum ToolLocation
{
    Left,
    Bottom,
}

// A tool panel (explorer, output, ...) docked around the document area.
public abstract partial class ToolViewModel : PaneViewModel
{
    protected ToolViewModel(string contentId, string title, ToolLocation location)
        : base(contentId, title)
    {
        Location = location;
        IsVisible = true;
    }

    // Where the panel is docked on first launch.
    public ToolLocation Location { get; }

    // Kept in sync with AvalonDock: false while the user has hidden the panel.
    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    // Shows the panel if hidden and gives it focus.
    public void Show()
    {
        IsVisible = true;
        IsActive = true;
    }
}

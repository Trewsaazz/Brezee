using CommunityToolkit.Mvvm.ComponentModel;

namespace Brezee.App.Shell;

// Base for anything hosted by the docking manager, whether a tool panel or a document.
public abstract partial class PaneViewModel : ObservableObject
{
    protected PaneViewModel(string contentId, string title)
    {
        ContentId = contentId;
        Title = title;
    }

    // Stable identifier used by AvalonDock to save and restore layouts.
    public string ContentId { get; }

    [ObservableProperty]
    public partial string Title { get; set; }
}

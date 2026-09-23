using CommunityToolkit.Mvvm.Input;

namespace Brezee.App.Shell;

// A document tab in the central area (welcome page, SQL editors, table views, ...).
public abstract partial class DocumentViewModel : PaneViewModel
{
    protected DocumentViewModel(string contentId, string title)
        : base(contentId, title)
    {
    }

    public event EventHandler? CloseRequested;

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
}

using Brezee.App.Shell;

namespace Brezee.App.Features.Explorer;

// Database explorer panel. Will hold the connection and object tree (Phase 0/1).
public sealed class ExplorerViewModel : ToolViewModel
{
    public ExplorerViewModel()
        : base("explorer", "Database Explorer", ToolLocation.Left)
    {
    }
}

using System.Collections.ObjectModel;
using Brezee.App.Resources;
using Brezee.App.Shell;

namespace Brezee.App.Features.Output;

// Output panel: a running log of what the application is doing.
public sealed class OutputViewModel : ToolViewModel
{
    public OutputViewModel()
        : base("output", Strings.Output_Title, ToolLocation.Bottom)
    {
    }

    public ObservableCollection<string> Lines { get; } = [];

    public void WriteLine(string message) =>
        Lines.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
}

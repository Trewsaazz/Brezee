using System.Collections.ObjectModel;
using Brezee.App.Features.Explorer;
using Brezee.App.Features.Output;
using Brezee.App.Features.Welcome;
using Brezee.Bridge;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Brezee.App.Shell;

public sealed partial class ShellViewModel : ObservableObject
{
    public ShellViewModel(ExplorerViewModel explorer, OutputViewModel output)
    {
        CoreVersion = CoreInfo.Version;

        Tools = [explorer, output];

        OpenDocument(new WelcomeViewModel(CoreVersion));

        output.WriteLine($"Brezee started (core v{CoreVersion}).");
    }

    public string CoreVersion { get; }

    public ObservableCollection<ToolViewModel> Tools { get; }

    public ObservableCollection<DocumentViewModel> Documents { get; } = [];

    [ObservableProperty]
    public partial DocumentViewModel? ActiveDocument { get; set; }

    public void OpenDocument(DocumentViewModel document)
    {
        if (!Documents.Contains(document))
        {
            document.CloseRequested += OnDocumentCloseRequested;
            Documents.Add(document);
        }

        ActiveDocument = document;
    }

    private void OnDocumentCloseRequested(object? sender, EventArgs e)
    {
        if (sender is not DocumentViewModel document)
            return;

        document.CloseRequested -= OnDocumentCloseRequested;
        Documents.Remove(document);
    }
}

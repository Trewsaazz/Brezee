using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Brezee.App.Commands;
using Brezee.App.Connections;
using Brezee.App.Features.Explorer;
using Brezee.App.Features.Output;
using Brezee.App.Features.TableData;
using Brezee.App.Features.Welcome;
using Brezee.App.Resources;
using Brezee.Bridge;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Brezee.App.Shell;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly WelcomeViewModel _welcome;
    private readonly ConnectionCoordinator _coordinator;
    private readonly ExplorerViewModel _explorer;

    public ShellViewModel(
        CommandRegistry commands,
        ConnectionCoordinator coordinator,
        ConnectionManager connections,
        RecentConnections recent,
        ExplorerViewModel explorer,
        OutputViewModel output,
        ILogger<ShellViewModel> logger)
    {
        Commands = commands;
        _coordinator = coordinator;
        _explorer = explorer;
        Recent = recent;

        explorer.OpenDataRequested += (_, node) => OpenTableData(node);
        connections.Removed += (_, connection) => CloseDocumentsOf(connection);
        CoreVersion = CoreInfo.Version;

        Tools = [explorer, output];

        RegisterCommands(explorer, output);

        _welcome = new WelcomeViewModel(CoreVersion, recent, OpenRecentCommand, Commands["file.connect"]);
        OpenDocument(_welcome);

        logger.LogInformation("Brezee started (core v{CoreVersion})", CoreVersion);
    }

    public CommandRegistry Commands { get; }

    // Recently used databases, newest first (File > Recent Connections and the welcome page).
    public RecentConnections Recent { get; }

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

    private void RegisterCommands(ExplorerViewModel explorer, OutputViewModel output)
    {
        Commands.Register("file.connect", Strings.Command_FileConnect,
            new RelayCommand(() => Connect(explorer)),
            new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift));

        Commands.Register("database.disconnect", Strings.Command_Disconnect,
            explorer.DisconnectCommand);

        Commands.Register("file.exit", Strings.Command_FileExit,
            new RelayCommand(() => Application.Current.Shutdown()));

        Commands.Register("view.explorer", Strings.Command_ViewExplorer,
            new RelayCommand(explorer.Show),
            new KeyGesture(Key.L, ModifierKeys.Control | ModifierKeys.Alt));

        Commands.Register("view.output", Strings.Command_ViewOutput,
            new RelayCommand(output.Show),
            new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt));

        Commands.Register("view.welcome", Strings.Command_ViewWelcome,
            new RelayCommand(() => OpenDocument(_welcome)));
    }

    private void Connect(ExplorerViewModel explorer)
    {
        if (_coordinator.ConnectNew() is not null)
            explorer.Show();
    }

    [RelayCommand]
    private async Task OpenRecentAsync(RecentConnection recent)
    {
        if (await _coordinator.ConnectRecentAsync(recent) is not null)
            _explorer.Show();
    }

    // Shows a table's or view's rows, reusing its tab if already open.
    private void OpenTableData(ObjectNode node)
    {
        if (node.Database.Active is not { } connection)
            return;

        var contentId = TableDataViewModel.ContentIdFor(connection, node.Name);
        if (Documents.FirstOrDefault(d => d.ContentId == contentId) is { } existing)
        {
            ActiveDocument = existing;
            return;
        }

        var document = new TableDataViewModel(connection, node.Database.Name, node.Name);
        OpenDocument(document);
        _ = document.LoadAsync();
    }

    // A disconnected database's documents cannot work any more.
    private void CloseDocumentsOf(ActiveConnection connection)
    {
        foreach (var document in Documents.OfType<TableDataViewModel>().Where(d => d.Connection == connection).ToList())
        {
            document.CloseRequested -= OnDocumentCloseRequested;
            Documents.Remove(document);
        }
    }

    private void OnDocumentCloseRequested(object? sender, EventArgs e)
    {
        if (sender is not DocumentViewModel document)
            return;

        document.CloseRequested -= OnDocumentCloseRequested;
        Documents.Remove(document);
    }
}

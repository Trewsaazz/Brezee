using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Brezee.App.Commands;
using Brezee.App.Connections;
using Brezee.App.Features.Explorer;
using Brezee.App.Features.Output;
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
    private readonly IDialogService _dialogs;
    private readonly ConnectionManager _connections;

    public ShellViewModel(
        CommandRegistry commands,
        IDialogService dialogs,
        ConnectionManager connections,
        ExplorerViewModel explorer,
        OutputViewModel output,
        ILogger<ShellViewModel> logger)
    {
        Commands = commands;
        _dialogs = dialogs;
        _connections = connections;
        CoreVersion = CoreInfo.Version;

        Tools = [explorer, output];

        _welcome = new WelcomeViewModel(CoreVersion);
        OpenDocument(_welcome);

        RegisterCommands(explorer, output);

        logger.LogInformation("Brezee started (core v{CoreVersion})", CoreVersion);
    }

    public CommandRegistry Commands { get; }

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
        if (_dialogs.ShowConnectDialog() is not { } session)
            return;

        _connections.Add(session);
        explorer.Show();
    }

    private void OnDocumentCloseRequested(object? sender, EventArgs e)
    {
        if (sender is not DocumentViewModel document)
            return;

        document.CloseRequested -= OnDocumentCloseRequested;
        Documents.Remove(document);
    }
}

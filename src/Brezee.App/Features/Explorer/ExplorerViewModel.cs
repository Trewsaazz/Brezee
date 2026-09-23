using System.Collections.ObjectModel;
using Brezee.App.Connections;
using Brezee.App.Resources;
using Brezee.App.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Brezee.App.Features.Explorer;

// Database explorer panel: the open connections (object trees come in Phase 1).
public sealed partial class ExplorerViewModel : ToolViewModel
{
    private readonly ConnectionManager _connections;

    public ExplorerViewModel(ConnectionManager connections)
        : base("explorer", Strings.Explorer_Title, ToolLocation.Left)
    {
        _connections = connections;
        _connections.SessionAdded += (_, session) => OnSessionAdded(session);
        _connections.SessionRemoved += (_, session) => OnSessionRemoved(session);

        foreach (var session in _connections.Sessions)
            OnSessionAdded(session);
    }

    public ObservableCollection<DatabaseNodeViewModel> Databases { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    public partial DatabaseNodeViewModel? SelectedDatabase { get; set; }

    public bool HasDatabases => Databases.Count > 0;

    private bool CanDisconnect(DatabaseNodeViewModel? node) => (node ?? SelectedDatabase) is not null;

    // Disconnects the given database, or the selected one when invoked from a menu.
    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private void Disconnect(DatabaseNodeViewModel? node)
    {
        if ((node ?? SelectedDatabase) is { } target)
            _connections.Disconnect(target.Session);
    }

    private void OnSessionAdded(IDatabaseSession session)
    {
        var node = new DatabaseNodeViewModel(session);
        Databases.Add(node);
        SelectedDatabase = node;
        OnPropertyChanged(nameof(HasDatabases));
    }

    private void OnSessionRemoved(IDatabaseSession session)
    {
        var node = Databases.FirstOrDefault(n => n.Session == session);
        if (node is null)
            return;

        Databases.Remove(node);
        if (SelectedDatabase == node)
            SelectedDatabase = Databases.LastOrDefault();
        OnPropertyChanged(nameof(HasDatabases));
    }
}

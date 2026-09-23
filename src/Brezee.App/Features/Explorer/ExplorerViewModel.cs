using System.Collections.ObjectModel;
using Brezee.App.Connections;
using Brezee.App.Resources;
using Brezee.App.Shell;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Brezee.App.Features.Explorer;

// Database explorer panel: saved and open databases (object trees come in Phase 1).
public sealed partial class ExplorerViewModel : ToolViewModel
{
    private readonly ConnectionManager _connections;
    private readonly SavedConnections _saved;
    private readonly ConnectionCoordinator _coordinator;

    public ExplorerViewModel(ConnectionManager connections, SavedConnections saved, ConnectionCoordinator coordinator)
        : base("explorer", Strings.Explorer_Title, ToolLocation.Left)
    {
        _connections = connections;
        _saved = saved;
        _coordinator = coordinator;

        foreach (var connection in _saved.Items)
            Databases.Add(new DatabaseNodeViewModel(connection, null));
        foreach (var active in _connections.Connections)
            OnConnected(active);

        _saved.Added += (_, connection) => OnSaved(connection);
        _saved.Removed += (_, connection) => OnUnsaved(connection);
        _connections.Added += (_, active) => OnConnected(active);
        _connections.Removed += (_, active) => OnDisconnected(active);
        Databases.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasDatabases));
    }

    public ObservableCollection<DatabaseNodeViewModel> Databases { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand), nameof(DisconnectCommand), nameof(RemoveCommand))]
    public partial DatabaseNodeViewModel? SelectedDatabase { get; set; }

    public bool HasDatabases => Databases.Count > 0;

    // Commands act on the node passed in (context menu, double-click) or else the selected one.
    private DatabaseNodeViewModel? Target(DatabaseNodeViewModel? node) => node ?? SelectedDatabase;

    private bool CanConnect(DatabaseNodeViewModel? node) => Target(node) is { IsSaved: true, IsConnected: false };

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private void Connect(DatabaseNodeViewModel? node)
    {
        if (Target(node)?.Saved is { } saved)
            _coordinator.Connect(saved);
    }

    private bool CanDisconnect(DatabaseNodeViewModel? node) => Target(node) is { IsConnected: true };

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private void Disconnect(DatabaseNodeViewModel? node)
    {
        if (Target(node)?.Active is { } active)
            _connections.Disconnect(active);
    }

    private bool CanRemove(DatabaseNodeViewModel? node) => Target(node) is { IsSaved: true };

    // Forgets a saved connection. An open connection to it stays open until disconnected.
    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove(DatabaseNodeViewModel? node)
    {
        if (Target(node)?.Saved is { } saved)
            _saved.Remove(saved);
    }

    private void OnSaved(SavedConnection connection)
    {
        // Normally saved before it connects, but attach to an open connection just in case.
        var active = _connections.FindBySavedConnection(connection.Id);
        var node = active is null ? null : Databases.FirstOrDefault(n => n.Active == active);
        if (node is not null)
            node.Saved = connection;
        else
            Databases.Add(node = new DatabaseNodeViewModel(connection, active));

        RefreshCommands();
    }

    private void OnUnsaved(SavedConnection connection)
    {
        var node = Databases.FirstOrDefault(n => n.Saved == connection);
        if (node is null)
            return;

        if (node.IsConnected)
            node.Saved = null;
        else
            RemoveNode(node);

        RefreshCommands();
    }

    private void OnConnected(ActiveConnection active)
    {
        var node = active.SavedConnectionId is { } id ? Databases.FirstOrDefault(n => n.Saved?.Id == id) : null;
        if (node is not null)
            node.Active = active;
        else
            Databases.Add(node = new DatabaseNodeViewModel(null, active));

        SelectedDatabase = node;
        RefreshCommands();
    }

    private void OnDisconnected(ActiveConnection active)
    {
        var node = Databases.FirstOrDefault(n => n.Active == active);
        if (node is null)
            return;

        if (node.IsSaved)
            node.Active = null;
        else
            RemoveNode(node);

        RefreshCommands();
    }

    private void RemoveNode(DatabaseNodeViewModel node)
    {
        Databases.Remove(node);
        if (SelectedDatabase == node)
            SelectedDatabase = Databases.LastOrDefault();
    }

    private void RefreshCommands()
    {
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
    }
}

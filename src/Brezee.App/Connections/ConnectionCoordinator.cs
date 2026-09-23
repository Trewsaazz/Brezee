using Brezee.App.Shell;

namespace Brezee.App.Connections;

// The "connect" workflows shared by the menu and the explorer: ask for details, open the session,
// remember it if the user chose to save it.
public sealed class ConnectionCoordinator(IDialogService dialogs, ConnectionManager connections, SavedConnections saved)
{
    // Connects to a database the user enters in the Connect dialog. Returns null if cancelled.
    public ActiveConnection? ConnectNew()
    {
        if (dialogs.ShowConnectDialog(null) is not { } result)
            return null;

        if (result.SavedAs is { } savedAs)
            saved.Add(savedAs);

        return connections.Add(result.Session, result.SavedAs?.Id);
    }

    // Connects to a saved database, asking only for what is not saved (the password).
    // Returns the existing connection if it is already open, or null if cancelled.
    public ActiveConnection? Connect(SavedConnection connection)
    {
        if (connections.FindBySavedConnection(connection.Id) is { } existing)
            return existing;

        if (dialogs.ShowConnectDialog(connection) is not { } result)
            return null;

        return connections.Add(result.Session, connection.Id);
    }
}

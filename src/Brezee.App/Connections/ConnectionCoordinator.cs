using Brezee.App.Shell;
using Brezee.Bridge;

namespace Brezee.App.Connections;

// The "connect" workflows shared by the menu and the explorer: ask for details, open the session,
// remember it (and its password, encrypted) if the user chose to.
public sealed class ConnectionCoordinator(
    IDialogService dialogs,
    IDatabaseConnector connector,
    ICredentialProtector protector,
    ConnectionManager connections,
    SavedConnections saved)
{
    private readonly HashSet<Guid> _connecting = [];

    // Connects to a database the user enters in the Connect dialog. Returns null if cancelled.
    public ActiveConnection? ConnectNew()
    {
        if (dialogs.ShowConnectDialog(null) is not { } result)
            return null;

        if (result.SavedAs is { } savedAs)
            saved.Add(savedAs);

        return connections.Add(result.Session, result.SavedAs?.Id);
    }

    // Connects to a saved database. With a remembered password it connects straight away; otherwise,
    // or if that password no longer works, it asks through the Connect dialog.
    // Returns the existing connection if it is already open, or null if cancelled.
    public async Task<ActiveConnection?> ConnectAsync(SavedConnection connection)
    {
        if (connections.FindBySavedConnection(connection.Id) is { } existing)
            return existing;

        // Ignore a second request (e.g. a double double-click) while the first is still connecting.
        if (!_connecting.Add(connection.Id))
            return null;

        try
        {
            string? error = null;
            if (connection.ProtectedPassword is { } protectedPassword && protector.Unprotect(protectedPassword) is { } password)
            {
                try
                {
                    var session = await connector.ConnectAsync(connection.ToSettings(password));
                    return connections.Add(session, connection.Id);
                }
                catch (CoreException ex)
                {
                    // E.g. the password was changed on the server: let the user fix it.
                    error = ex.Message;
                }
            }

            if (dialogs.ShowConnectDialog(connection, error) is not { } result)
                return null;

            if (result.SavedAs is { } updated)
                saved.Update(updated);

            return connections.Add(result.Session, connection.Id);
        }
        finally
        {
            _connecting.Remove(connection.Id);
        }
    }

    // Removes the remembered password of a saved connection.
    public void ForgetPassword(SavedConnection connection)
    {
        if (connection.HasSavedPassword)
            saved.Update(connection with { ProtectedPassword = null });
    }
}

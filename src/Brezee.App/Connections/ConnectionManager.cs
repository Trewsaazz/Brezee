using Microsoft.Extensions.Logging;

namespace Brezee.App.Connections;

// An open session, and the saved connection it was opened from (if any).
public sealed class ActiveConnection(IDatabaseSession session, Guid? savedConnectionId)
{
    public IDatabaseSession Session { get; } = session;

    public Guid? SavedConnectionId { get; } = savedConnectionId;
}

// Owns every open database session. Several databases can be open at the same time.
public sealed class ConnectionManager(ILogger<ConnectionManager> logger) : IDisposable
{
    private readonly List<ActiveConnection> _connections = [];

    public IReadOnlyList<ActiveConnection> Connections => _connections;

    public event EventHandler<ActiveConnection>? Added;

    public event EventHandler<ActiveConnection>? Removed;

    public ActiveConnection? FindBySavedConnection(Guid savedConnectionId) =>
        _connections.FirstOrDefault(c => c.SavedConnectionId == savedConnectionId);

    public ActiveConnection Add(IDatabaseSession session, Guid? savedConnectionId = null)
    {
        var connection = new ActiveConnection(session, savedConnectionId);
        _connections.Add(connection);
        logger.LogInformation("Connected to {Database} ({Server})",
            session.Settings.Database, session.Details.ServerVersion);
        Added?.Invoke(this, connection);
        return connection;
    }

    public void Disconnect(ActiveConnection connection)
    {
        if (!_connections.Remove(connection))
            return;

        Removed?.Invoke(this, connection);

        var settings = connection.Session.Settings;
        try
        {
            connection.Session.Dispose();
            logger.LogInformation("Disconnected from {Database}", settings.Database);
        }
        catch (Exception ex)
        {
            // A failed disconnect must not stop the others from closing.
            logger.LogWarning(ex, "Error while disconnecting from {Database}", settings.Database);
        }
    }

    // Disconnects everything, e.g. when the app exits. Safe to call more than once.
    public void Dispose()
    {
        foreach (var connection in _connections.ToList())
            Disconnect(connection);
    }
}

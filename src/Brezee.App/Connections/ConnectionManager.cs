using Microsoft.Extensions.Logging;

namespace Brezee.App.Connections;

// Owns every open database session. Several databases can be open at the same time.
public sealed class ConnectionManager(ILogger<ConnectionManager> logger) : IDisposable
{
    private readonly List<IDatabaseSession> _sessions = [];

    public IReadOnlyList<IDatabaseSession> Sessions => _sessions;

    public event EventHandler<IDatabaseSession>? SessionAdded;

    public event EventHandler<IDatabaseSession>? SessionRemoved;

    public void Add(IDatabaseSession session)
    {
        _sessions.Add(session);
        logger.LogInformation("Connected to {Database} ({Server})",
            session.Settings.Database, session.Details.ServerVersion);
        SessionAdded?.Invoke(this, session);
    }

    public void Disconnect(IDatabaseSession session)
    {
        if (!_sessions.Remove(session))
            return;

        SessionRemoved?.Invoke(this, session);

        try
        {
            session.Dispose();
            logger.LogInformation("Disconnected from {Database}", session.Settings.Database);
        }
        catch (Exception ex)
        {
            // A failed disconnect must not stop the others from closing.
            logger.LogWarning(ex, "Error while disconnecting from {Database}", session.Settings.Database);
        }
    }

    // Disconnects everything, e.g. when the app exits. Safe to call more than once.
    public void Dispose()
    {
        foreach (var session in _sessions.ToList())
            Disconnect(session);
    }
}

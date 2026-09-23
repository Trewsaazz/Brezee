using Brezee.Bridge;

namespace Brezee.App.Connections;

// An open database connection as the app sees it. Dispose to disconnect.
public interface IDatabaseSession : IDisposable
{
    ConnectionSettings Settings { get; }

    DatabaseDetails Details { get; }

    // Lists the objects of one type without blocking the UI. Throws CoreException on failure.
    Task<IReadOnlyList<DatabaseObjectInfo>> ListObjectsAsync(
        DatabaseObjectType type, bool includeSystem = false, CancellationToken cancellationToken = default);
}

// Opens database sessions. Abstracted so view models can be tested without a Firebird server.
public interface IDatabaseConnector
{
    // Connects without blocking the UI. Throws CoreException when Firebird refuses.
    Task<IDatabaseSession> ConnectAsync(ConnectionSettings settings, CancellationToken cancellationToken = default);
}

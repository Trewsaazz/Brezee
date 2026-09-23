using Brezee.Bridge;

namespace Brezee.App.Connections;

// Connects through the native core.
public sealed class FirebirdConnector : IDatabaseConnector
{
    public Task<IDatabaseSession> ConnectAsync(ConnectionSettings settings, CancellationToken cancellationToken = default) =>
        // Attaching is a blocking network call, so it runs off the UI thread.
        Task.Run<IDatabaseSession>(() =>
        {
            var connection = DatabaseConnection.Open(settings);
            try
            {
                return new Session(connection, settings, connection.GetDetails());
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }, cancellationToken);

    private sealed class Session(DatabaseConnection connection, ConnectionSettings settings, DatabaseDetails details)
        : IDatabaseSession
    {
        public ConnectionSettings Settings { get; } = settings;

        public DatabaseDetails Details { get; } = details;

        public Task<IReadOnlyList<DatabaseObjectInfo>> ListObjectsAsync(
            DatabaseObjectType type, bool includeSystem = false, CancellationToken cancellationToken = default) =>
            Task.Run(() => connection.ListObjects(type, includeSystem), cancellationToken);

        public void Dispose() => connection.Dispose();
    }
}

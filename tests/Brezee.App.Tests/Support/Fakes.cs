using Brezee.App.Connections;
using Brezee.App.Shell;
using Brezee.Bridge;

namespace Brezee.App.Tests.Support;

public sealed class FakeSession(string database = "C:/data/employee.fdb", string host = "localhost") : IDatabaseSession
{
    public ConnectionSettings Settings { get; } = new() { Host = host, Database = database, User = "SYSDBA" };

    public DatabaseDetails Details { get; } = new()
    {
        ServerVersion = "WI-V5.0.4.1812 Firebird 5.0",
        OdsMajor = 13,
        OdsMinor = 1,
        PageSize = 16384,
        SqlDialect = 3,
    };

    public int DisposeCount { get; private set; }

    public bool ThrowOnDispose { get; init; }

    public void Dispose()
    {
        DisposeCount++;
        if (ThrowOnDispose)
            throw new CoreException("Connection lost", CoreErrorKind.Connection);
    }
}

// Succeeds with a FakeSession, or fails with the configured exception.
public sealed class FakeConnector : IDatabaseConnector
{
    public Exception? Failure { get; set; }

    public ConnectionSettings? LastSettings { get; private set; }

    public Task<IDatabaseSession> ConnectAsync(ConnectionSettings settings, CancellationToken cancellationToken = default)
    {
        LastSettings = settings;
        return Failure is null
            ? Task.FromResult<IDatabaseSession>(new FakeSession(settings.Database, settings.Host))
            : Task.FromException<IDatabaseSession>(Failure);
    }
}

public sealed class FakeDialogService : IDialogService
{
    // What the Connect dialog "returns": a session, or null for Cancel.
    public IDatabaseSession? ConnectResult { get; set; }

    public int ConnectDialogShown { get; private set; }

    public IDatabaseSession? ShowConnectDialog()
    {
        ConnectDialogShown++;
        return ConnectResult;
    }
}

using System.IO;
using Brezee.App.Connections;
using Brezee.App.Shell;
using Brezee.Bridge;
using Microsoft.Extensions.Logging.Abstractions;

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
    // What the Connect dialog "returns": a result, or null for Cancel.
    public ConnectResult? ConnectResult { get; set; }

    public List<SavedConnection?> ConnectDialogPrefills { get; } = [];

    public List<string?> ConnectDialogErrors { get; } = [];

    public ConnectResult? ShowConnectDialog(SavedConnection? prefill, string? error = null)
    {
        ConnectDialogPrefills.Add(prefill);
        ConnectDialogErrors.Add(error);
        return ConnectResult;
    }
}

// Reversible stand-in for DPAPI, so tests can see what would be stored.
public sealed class FakeProtector : ICredentialProtector
{
    public string Protect(string password) => "protected:" + password;

    public string? Unprotect(string protectedPassword) =>
        protectedPassword.StartsWith("protected:", StringComparison.Ordinal) ? protectedPassword["protected:".Length..] : null;
}

// A SavedConnections backed by a temporary file, deleted on dispose.
public sealed class TempSavedConnections : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"brezee-tests-{Guid.NewGuid():N}");

    public TempSavedConnections()
    {
        Store = new ConnectionStore(Path.Combine(_directory, "connections.json"), NullLogger<ConnectionStore>.Instance);
        Saved = new SavedConnections(Store, NullLogger<SavedConnections>.Instance);
    }

    public ConnectionStore Store { get; }

    public SavedConnections Saved { get; }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}

public static class TestData
{
    public static ConnectionCoordinator Coordinator(
        IDialogService dialogs, ConnectionManager connections, SavedConnections saved, IDatabaseConnector? connector = null) =>
        new(dialogs, connector ?? new FakeConnector(), new FakeProtector(), connections, saved);

    public static SavedConnection Saved(string name = "Employee", string database = "C:/data/employee.fdb", string host = "localhost") =>
        new() { Name = name, Database = database, Host = host, IsLocal = host.Length == 0 };
}

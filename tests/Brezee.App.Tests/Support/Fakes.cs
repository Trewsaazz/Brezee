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

    // Objects served by ListObjectsAsync, per type. Types not listed have none.
    public Dictionary<DatabaseObjectType, List<DatabaseObjectInfo>> Objects { get; } = [];

    // When set, ListObjectsAsync fails with this.
    public Exception? ListFailure { get; set; }

    public int ListCalls { get; private set; }

    public Task<IReadOnlyList<DatabaseObjectInfo>> ListObjectsAsync(
        DatabaseObjectType type, bool includeSystem = false, CancellationToken cancellationToken = default)
    {
        ListCalls++;
        if (ListFailure is not null)
            return Task.FromException<IReadOnlyList<DatabaseObjectInfo>>(ListFailure);

        IReadOnlyList<DatabaseObjectInfo> objects = Objects.TryGetValue(type, out var list) ? list.ToList() : [];
        return Task.FromResult(objects);
    }

    public FakeSession WithObjects(DatabaseObjectType type, params string[] names)
    {
        Objects[type] = names.Select(name => new DatabaseObjectInfo
        {
            Type = type,
            Name = name,
            Parent = string.Empty,
            Description = string.Empty,
            Flags = [],
        }).ToList();
        return this;
    }

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

    public List<bool> ConnectDialogPrefillIsSaved { get; } = [];

    public ConnectResult? ShowConnectDialog(SavedConnection? prefill, string? error = null, bool prefillIsSaved = true)
    {
        ConnectDialogPrefills.Add(prefill);
        ConnectDialogErrors.Add(error);
        ConnectDialogPrefillIsSaved.Add(prefillIsSaved);
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
        RecentStore = new RecentConnectionStore(Path.Combine(_directory, "recent.json"), NullLogger<RecentConnectionStore>.Instance);
    }

    public ConnectionStore Store { get; }

    public SavedConnections Saved { get; }

    public RecentConnectionStore RecentStore { get; }

    public ManualTime Time { get; } = new();

    public RecentConnections CreateRecent(ConnectionManager connections) => new(RecentStore, connections, Saved, Time);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}

// A clock the test controls; each read moves it one minute forward so entries get distinct times.
public sealed class ManualTime : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now = Now.AddMinutes(1);
}

public static class TestData
{
    public static ConnectionCoordinator Coordinator(
        IDialogService dialogs, ConnectionManager connections, SavedConnections saved, IDatabaseConnector? connector = null) =>
        new(dialogs, connector ?? new FakeConnector(), new FakeProtector(), connections, saved);

    public static SavedConnection Saved(string name = "Employee", string database = "C:/data/employee.fdb", string host = "localhost") =>
        new() { Name = name, Database = database, Host = host, IsLocal = host.Length == 0 };
}

using Brezee.App.Connections;
using Brezee.App.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Connections;

public class ConnectionManagerTests
{
    private readonly ConnectionManager _manager = new(NullLogger<ConnectionManager>.Instance);

    [Fact]
    public void Add_TracksConnectionAndRaisesEvent()
    {
        var session = new FakeSession();
        ActiveConnection? added = null;
        _manager.Added += (_, c) => added = c;

        var connection = _manager.Add(session);

        Assert.Same(connection, Assert.Single(_manager.Connections));
        Assert.Same(connection, added);
        Assert.Same(session, connection.Session);
        Assert.Null(connection.SavedConnectionId);
    }

    [Fact]
    public void Add_RemembersTheSavedConnectionItCameFrom()
    {
        var savedId = Guid.NewGuid();

        var connection = _manager.Add(new FakeSession(), savedId);

        Assert.Equal(savedId, connection.SavedConnectionId);
        Assert.Same(connection, _manager.FindBySavedConnection(savedId));
        Assert.Null(_manager.FindBySavedConnection(Guid.NewGuid()));
    }

    [Fact]
    public void SeveralDatabasesCanBeOpenAtOnce()
    {
        _manager.Add(new FakeSession("a.fdb"));
        _manager.Add(new FakeSession("b.fdb"));

        Assert.Equal(2, _manager.Connections.Count);
    }

    [Fact]
    public void Disconnect_RemovesAndDisposesSession()
    {
        var session = new FakeSession();
        var connection = _manager.Add(session);
        ActiveConnection? removed = null;
        _manager.Removed += (_, c) => removed = c;

        _manager.Disconnect(connection);

        Assert.Empty(_manager.Connections);
        Assert.Equal(1, session.DisposeCount);
        Assert.Same(connection, removed);
    }

    [Fact]
    public void Disconnect_UnknownConnection_DoesNothing()
    {
        var session = new FakeSession();

        _manager.Disconnect(new ActiveConnection(session, null));

        Assert.Equal(0, session.DisposeCount);
    }

    [Fact]
    public void Dispose_ClosesEverySessionEvenIfOneFails()
    {
        var failing = new FakeSession("a.fdb") { ThrowOnDispose = true };
        var healthy = new FakeSession("b.fdb");
        _manager.Add(failing);
        _manager.Add(healthy);

        _manager.Dispose();
        _manager.Dispose(); // A second call is harmless.

        Assert.Empty(_manager.Connections);
        Assert.Equal(1, failing.DisposeCount);
        Assert.Equal(1, healthy.DisposeCount);
    }
}

using Brezee.App.Connections;
using Brezee.App.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Connections;

public class ConnectionManagerTests
{
    private readonly ConnectionManager _manager = new(NullLogger<ConnectionManager>.Instance);

    [Fact]
    public void Add_TracksSessionAndRaisesEvent()
    {
        var session = new FakeSession();
        IDatabaseSession? added = null;
        _manager.SessionAdded += (_, s) => added = s;

        _manager.Add(session);

        Assert.Same(session, Assert.Single(_manager.Sessions));
        Assert.Same(session, added);
    }

    [Fact]
    public void SeveralDatabasesCanBeOpenAtOnce()
    {
        _manager.Add(new FakeSession("a.fdb"));
        _manager.Add(new FakeSession("b.fdb"));

        Assert.Equal(2, _manager.Sessions.Count);
    }

    [Fact]
    public void Disconnect_RemovesAndDisposesSession()
    {
        var session = new FakeSession();
        _manager.Add(session);
        IDatabaseSession? removed = null;
        _manager.SessionRemoved += (_, s) => removed = s;

        _manager.Disconnect(session);

        Assert.Empty(_manager.Sessions);
        Assert.Equal(1, session.DisposeCount);
        Assert.Same(session, removed);
    }

    [Fact]
    public void Disconnect_UnknownSession_DoesNothing()
    {
        var session = new FakeSession();

        _manager.Disconnect(session);

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

        Assert.Empty(_manager.Sessions);
        Assert.Equal(1, failing.DisposeCount);
        Assert.Equal(1, healthy.DisposeCount);
    }
}

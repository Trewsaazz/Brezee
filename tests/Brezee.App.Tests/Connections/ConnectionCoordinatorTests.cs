using Brezee.App.Connections;
using Brezee.App.Shell;
using Brezee.App.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Connections;

public sealed class ConnectionCoordinatorTests : IDisposable
{
    private readonly FakeDialogService _dialogs = new();
    private readonly ConnectionManager _connections = new(NullLogger<ConnectionManager>.Instance);
    private readonly TempSavedConnections _temp = new();
    private readonly ConnectionCoordinator _coordinator;

    public ConnectionCoordinatorTests()
    {
        _coordinator = new ConnectionCoordinator(_dialogs, _connections, _temp.Saved);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void ConnectNew_Cancelled_DoesNothing()
    {
        Assert.Null(_coordinator.ConnectNew());

        Assert.Empty(_connections.Connections);
        Assert.Null(Assert.Single(_dialogs.ConnectDialogPrefills));
    }

    [Fact]
    public void ConnectNew_WithoutSaving_OpensAnUnsavedConnection()
    {
        _dialogs.ConnectResult = new ConnectResult(new FakeSession(), SavedAs: null);

        var connection = _coordinator.ConnectNew();

        Assert.NotNull(connection);
        Assert.Null(connection.SavedConnectionId);
        Assert.Empty(_temp.Saved.Items);
    }

    [Fact]
    public void ConnectNew_WithSaving_SavesAndLinksTheConnection()
    {
        var savedAs = TestData.Saved();
        _dialogs.ConnectResult = new ConnectResult(new FakeSession(), savedAs);

        var connection = _coordinator.ConnectNew();

        Assert.Same(savedAs, Assert.Single(_temp.Saved.Items));
        Assert.Equal(savedAs.Id, connection?.SavedConnectionId);
    }

    [Fact]
    public void Connect_Saved_PrefillsTheDialogAndLinksTheConnection()
    {
        var saved = TestData.Saved();
        _temp.Saved.Add(saved);
        _dialogs.ConnectResult = new ConnectResult(new FakeSession(), SavedAs: null);

        var connection = _coordinator.Connect(saved);

        Assert.Same(saved, Assert.Single(_dialogs.ConnectDialogPrefills));
        Assert.Equal(saved.Id, connection?.SavedConnectionId);
    }

    [Fact]
    public void Connect_SavedThatIsAlreadyOpen_ReturnsTheOpenConnection()
    {
        var saved = TestData.Saved();
        var open = _connections.Add(new FakeSession(), saved.Id);

        var connection = _coordinator.Connect(saved);

        Assert.Same(open, connection);
        Assert.Empty(_dialogs.ConnectDialogPrefills);
        Assert.Single(_connections.Connections);
    }
}

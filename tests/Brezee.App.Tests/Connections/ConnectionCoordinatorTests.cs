using Brezee.App.Connections;
using Brezee.App.Shell;
using Brezee.App.Tests.Support;
using Brezee.Bridge;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Connections;

public sealed class ConnectionCoordinatorTests : IDisposable
{
    private readonly FakeDialogService _dialogs = new();
    private readonly FakeConnector _connector = new();
    private readonly ConnectionManager _connections = new(NullLogger<ConnectionManager>.Instance);
    private readonly TempSavedConnections _temp = new();
    private readonly ConnectionCoordinator _coordinator;

    public ConnectionCoordinatorTests()
    {
        _coordinator = TestData.Coordinator(_dialogs, _connections, _temp.Saved, _connector);
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
    public async Task Connect_SavedWithoutPassword_AsksThroughThePrefilledDialog()
    {
        var saved = TestData.Saved();
        _temp.Saved.Add(saved);
        _dialogs.ConnectResult = new ConnectResult(new FakeSession(), SavedAs: null);

        var connection = await _coordinator.ConnectAsync(saved);

        Assert.Same(saved, Assert.Single(_dialogs.ConnectDialogPrefills));
        Assert.Null(Assert.Single(_dialogs.ConnectDialogErrors));
        Assert.Null(_connector.LastSettings); // The dialog did the connecting.
        Assert.Equal(saved.Id, connection?.SavedConnectionId);
    }

    [Fact]
    public async Task Connect_SavedWithRememberedPassword_ConnectsWithoutAsking()
    {
        var saved = TestData.Saved() with { ProtectedPassword = "protected:secret" };
        _temp.Saved.Add(saved);

        var connection = await _coordinator.ConnectAsync(saved);

        Assert.Empty(_dialogs.ConnectDialogPrefills);
        Assert.Equal("secret", _connector.LastSettings?.Password);
        Assert.Equal(saved.Database, _connector.LastSettings?.Database);
        Assert.Equal(saved.Id, connection?.SavedConnectionId);
    }

    [Fact]
    public async Task Connect_RememberedPasswordRejected_AsksAgainShowingWhy()
    {
        var saved = TestData.Saved() with { ProtectedPassword = "protected:old-secret" };
        _temp.Saved.Add(saved);
        _connector.Failure = new CoreException("Your user name and password are not defined", CoreErrorKind.Connection);
        var updated = saved with { ProtectedPassword = "protected:new-secret" };
        _dialogs.ConnectResult = new ConnectResult(new FakeSession(), updated);

        var connection = await _coordinator.ConnectAsync(saved);

        Assert.Equal("Your user name and password are not defined", Assert.Single(_dialogs.ConnectDialogErrors));
        Assert.NotNull(connection);
        Assert.Equal("protected:new-secret", Assert.Single(_temp.Store.Load()).ProtectedPassword);
    }

    [Fact]
    public async Task Connect_PasswordFromAnotherWindowsAccount_FallsBackToTheDialog()
    {
        var saved = TestData.Saved() with { ProtectedPassword = "not-decryptable-here" };
        _temp.Saved.Add(saved);

        Assert.Null(await _coordinator.ConnectAsync(saved)); // Dialog cancelled.

        Assert.Null(_connector.LastSettings);
        Assert.Single(_dialogs.ConnectDialogPrefills);
    }

    [Fact]
    public async Task Connect_SavedThatIsAlreadyOpen_ReturnsTheOpenConnection()
    {
        var saved = TestData.Saved();
        var open = _connections.Add(new FakeSession(), saved.Id);

        var connection = await _coordinator.ConnectAsync(saved);

        Assert.Same(open, connection);
        Assert.Empty(_dialogs.ConnectDialogPrefills);
        Assert.Single(_connections.Connections);
    }

    [Fact]
    public void ForgetPassword_ClearsOnlyThePassword()
    {
        var saved = TestData.Saved() with { ProtectedPassword = "protected:secret" };
        _temp.Saved.Add(saved);

        _coordinator.ForgetPassword(saved);

        var stored = Assert.Single(_temp.Store.Load());
        Assert.Null(stored.ProtectedPassword);
        Assert.Equal(saved.Name, stored.Name);
        Assert.Equal(saved.Id, stored.Id);
    }

    [Fact]
    public async Task ConnectRecent_OfASavedConnection_UsesItsRememberedPassword()
    {
        var saved = TestData.Saved() with { ProtectedPassword = "protected:secret" };
        _temp.Saved.Add(saved);
        var recent = new RecentConnection { Name = saved.Name, Database = saved.Database, SavedConnectionId = saved.Id };

        var connection = await _coordinator.ConnectRecentAsync(recent);

        Assert.Empty(_dialogs.ConnectDialogPrefills);
        Assert.Equal("secret", _connector.LastSettings?.Password);
        Assert.Equal(saved.Id, connection?.SavedConnectionId);
    }

    [Fact]
    public async Task ConnectRecent_Unsaved_OpensThePrefilledDialogAsANewConnection()
    {
        var recent = new RecentConnection { Name = "stock.fdb", Database = @"C:\Data\stock.fdb", IsLocal = true, User = "ALICE" };
        _dialogs.ConnectResult = new ConnectResult(new FakeSession(), SavedAs: null);

        await _coordinator.ConnectRecentAsync(recent);

        var prefill = Assert.Single(_dialogs.ConnectDialogPrefills);
        Assert.Equal(@"C:\Data\stock.fdb", prefill?.Database);
        Assert.Equal("ALICE", prefill?.User);
        Assert.False(Assert.Single(_dialogs.ConnectDialogPrefillIsSaved));
    }

    [Fact]
    public async Task ConnectRecent_WhoseSavedConnectionWasRemoved_FallsBackToTheDialog()
    {
        var recent = new RecentConnection { Name = "Gone", Database = "gone.fdb", SavedConnectionId = Guid.NewGuid() };

        Assert.Null(await _coordinator.ConnectRecentAsync(recent)); // Dialog cancelled.

        Assert.False(Assert.Single(_dialogs.ConnectDialogPrefillIsSaved));
    }
}

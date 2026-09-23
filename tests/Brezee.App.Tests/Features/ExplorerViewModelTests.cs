using Brezee.App.Connections;
using Brezee.App.Features.Explorer;
using Brezee.App.Shell;
using Brezee.App.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Features;

public sealed class ExplorerViewModelTests : IDisposable
{
    private readonly FakeDialogService _dialogs = new();
    private readonly ConnectionManager _connections = new(NullLogger<ConnectionManager>.Instance);
    private readonly TempSavedConnections _temp = new();

    public void Dispose() => _temp.Dispose();

    private ExplorerViewModel CreateExplorer() =>
        new(_connections, _temp.Saved, new ConnectionCoordinator(_dialogs, _connections, _temp.Saved));

    [Fact]
    public void StartsEmpty()
    {
        var explorer = CreateExplorer();

        Assert.Empty(explorer.Databases);
        Assert.False(explorer.HasDatabases);
        Assert.False(explorer.DisconnectCommand.CanExecute(null));
    }

    [Fact]
    public void SavedConnections_AreListedAsNotConnected()
    {
        _temp.Saved.Add(TestData.Saved("Employee", host: "db.example.com"));

        var node = Assert.Single(CreateExplorer().Databases);

        Assert.Equal("Employee", node.Name);
        Assert.Equal("db.example.com:3050", node.Location);
        Assert.Equal("not connected", node.Status);
        Assert.False(node.IsConnected);
        Assert.True(node.IsSaved);
    }

    [Fact]
    public void UnsavedConnection_AppearsWhileOpenAndIsSelected()
    {
        var explorer = CreateExplorer();

        var active = _connections.Add(new FakeSession("C:/data/employee.fdb", "db.example.com"));

        var node = Assert.Single(explorer.Databases);
        Assert.Same(node, explorer.SelectedDatabase);
        Assert.Equal("employee.fdb", node.Name);
        Assert.Contains("Firebird 5.0", node.Status);

        _connections.Disconnect(active);
        Assert.Empty(explorer.Databases);
    }

    [Fact]
    public void ConnectingASavedConnection_MarksItsNodeConnected()
    {
        var saved = TestData.Saved();
        _temp.Saved.Add(saved);
        var explorer = CreateExplorer();

        var active = _connections.Add(new FakeSession(), saved.Id);

        var node = Assert.Single(explorer.Databases);
        Assert.True(node.IsConnected);
        Assert.Contains("Firebird 5.0", node.Status);

        _connections.Disconnect(active);
        Assert.False(Assert.Single(explorer.Databases).IsConnected); // Stays listed: it is saved.
    }

    [Fact]
    public void Connect_OnSavedNode_AsksForTheRestThroughTheDialog()
    {
        var saved = TestData.Saved();
        _temp.Saved.Add(saved);
        _dialogs.ConnectResult = new ConnectResult(new FakeSession(), SavedAs: null);
        var explorer = CreateExplorer();
        var node = explorer.Databases[0];

        Assert.True(explorer.ConnectCommand.CanExecute(node));
        explorer.ConnectCommand.Execute(node);

        Assert.Same(saved, Assert.Single(_dialogs.ConnectDialogPrefills));
        Assert.True(node.IsConnected);
        Assert.False(explorer.ConnectCommand.CanExecute(node)); // Already connected.
    }

    [Theory]
    [InlineData("employee", "employee")]
    [InlineData(@"C:\Data\Sales.FDB", "Sales.FDB")]
    [InlineData("/var/lib/firebird/data/stock.fdb", "stock.fdb")]
    public void UnsavedName_IsTheFileNameOrAlias(string database, string expected)
    {
        var explorer = CreateExplorer();

        _connections.Add(new FakeSession(database));

        Assert.Equal(expected, Assert.Single(explorer.Databases).Name);
    }

    [Fact]
    public void LocalDatabase_IsLabelledLocal()
    {
        _temp.Saved.Add(TestData.Saved(database: @"C:\Data\stock.fdb", host: ""));

        Assert.Equal("local", Assert.Single(CreateExplorer().Databases).Location);
    }

    [Fact]
    public void Disconnect_WithoutArgument_ClosesSelectedDatabase()
    {
        var explorer = CreateExplorer();
        var first = new FakeSession("a.fdb");
        var second = new FakeSession("b.fdb");
        _connections.Add(first);
        _connections.Add(second);

        explorer.DisconnectCommand.Execute(null);

        Assert.Equal(1, second.DisposeCount);
        Assert.Equal("a.fdb", Assert.Single(explorer.Databases).Name);
        Assert.Equal("a.fdb", explorer.SelectedDatabase?.Name);
    }

    [Fact]
    public void Remove_ForgetsASavedConnection()
    {
        var saved = TestData.Saved();
        _temp.Saved.Add(saved);
        var explorer = CreateExplorer();

        explorer.RemoveCommand.Execute(explorer.Databases[0]);

        Assert.Empty(explorer.Databases);
        Assert.Empty(_temp.Store.Load());
    }

    [Fact]
    public void Remove_WhileConnected_KeepsTheOpenConnectionListed()
    {
        var saved = TestData.Saved();
        _temp.Saved.Add(saved);
        var explorer = CreateExplorer();
        _connections.Add(new FakeSession(), saved.Id);

        explorer.RemoveCommand.Execute(explorer.Databases[0]);

        var node = Assert.Single(explorer.Databases);
        Assert.True(node.IsConnected);
        Assert.False(node.IsSaved);
        Assert.False(explorer.RemoveCommand.CanExecute(node));
    }

    [Fact]
    public void SavingAConnectionThatIsAlreadyOpen_MergesIntoOneNode()
    {
        var saved = TestData.Saved();
        var explorer = CreateExplorer();
        _connections.Add(new FakeSession(), saved.Id);

        _temp.Saved.Add(saved);

        var node = Assert.Single(explorer.Databases);
        Assert.True(node.IsSaved);
        Assert.True(node.IsConnected);
    }
}

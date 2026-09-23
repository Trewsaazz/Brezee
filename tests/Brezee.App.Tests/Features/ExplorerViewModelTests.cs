using Brezee.App.Connections;
using Brezee.App.Features.Explorer;
using Brezee.App.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Features;

public class ExplorerViewModelTests
{
    private readonly ConnectionManager _connections = new(NullLogger<ConnectionManager>.Instance);
    private readonly ExplorerViewModel _explorer;

    public ExplorerViewModelTests()
    {
        _explorer = new ExplorerViewModel(_connections);
    }

    [Fact]
    public void StartsEmpty()
    {
        Assert.Empty(_explorer.Databases);
        Assert.False(_explorer.HasDatabases);
        Assert.False(_explorer.DisconnectCommand.CanExecute(null));
    }

    [Fact]
    public void NewConnection_AppearsAndIsSelected()
    {
        _connections.Add(new FakeSession("C:/data/employee.fdb", "db.example.com"));

        var node = Assert.Single(_explorer.Databases);
        Assert.Same(node, _explorer.SelectedDatabase);
        Assert.True(_explorer.HasDatabases);
        Assert.Equal("employee.fdb", node.Name);
        Assert.Equal("db.example.com:3050", node.Location);
        Assert.Contains("Firebird 5.0", node.ServerVersion);
    }

    [Theory]
    [InlineData("employee", "employee")]
    [InlineData(@"C:\Data\Sales.FDB", "Sales.FDB")]
    [InlineData("/var/lib/firebird/data/stock.fdb", "stock.fdb")]
    public void Name_IsTheFileNameOrAlias(string database, string expected)
    {
        _connections.Add(new FakeSession(database));

        Assert.Equal(expected, Assert.Single(_explorer.Databases).Name);
    }

    [Fact]
    public void LocalConnection_IsLabelledLocal()
    {
        _connections.Add(new FakeSession("C:/data/employee.fdb", host: ""));

        Assert.Equal("local", Assert.Single(_explorer.Databases).Location);
    }

    [Fact]
    public void Disconnect_WithoutArgument_ClosesSelectedDatabase()
    {
        var first = new FakeSession("a.fdb");
        var second = new FakeSession("b.fdb");
        _connections.Add(first);
        _connections.Add(second);

        _explorer.DisconnectCommand.Execute(null);

        Assert.Equal(1, second.DisposeCount);
        Assert.Equal("a.fdb", Assert.Single(_explorer.Databases).Name);
        Assert.Equal("a.fdb", _explorer.SelectedDatabase?.Name);
    }

    [Fact]
    public void Disconnect_WithNode_ClosesThatDatabase()
    {
        var first = new FakeSession("a.fdb");
        _connections.Add(first);
        _connections.Add(new FakeSession("b.fdb"));

        _explorer.DisconnectCommand.Execute(_explorer.Databases[0]);

        Assert.Equal(1, first.DisposeCount);
        Assert.Equal("b.fdb", Assert.Single(_explorer.Databases).Name);
    }

    [Fact]
    public void ExistingConnections_AreShownWhenTheExplorerIsCreated()
    {
        var connections = new ConnectionManager(NullLogger<ConnectionManager>.Instance);
        connections.Add(new FakeSession("a.fdb"));

        var explorer = new ExplorerViewModel(connections);

        Assert.Single(explorer.Databases);
    }
}

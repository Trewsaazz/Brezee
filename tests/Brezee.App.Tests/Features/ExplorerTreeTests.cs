using Brezee.App.Connections;
using Brezee.App.Features.Explorer;
using Brezee.App.Tests.Support;
using Brezee.Bridge;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Features;

public sealed class ExplorerTreeTests : IDisposable
{
    private readonly ConnectionManager _connections = new(NullLogger<ConnectionManager>.Instance);
    private readonly TempSavedConnections _temp = new();
    private readonly ExplorerViewModel _explorer;

    public ExplorerTreeTests()
    {
        _explorer = new ExplorerViewModel(_connections, _temp.Saved,
            TestData.Coordinator(new FakeDialogService(), _connections, _temp.Saved));
    }

    public void Dispose() => _temp.Dispose();

    private DatabaseNodeViewModel Connect(FakeSession session)
    {
        _connections.Add(session);
        return Assert.Single(_explorer.Databases);
    }

    private static ObjectFolderNode Folder(DatabaseNodeViewModel database, DatabaseObjectType type) =>
        database.Children.OfType<ObjectFolderNode>().Single(f => f.Type == type);

    [Fact]
    public void ConnectedDatabase_ShowsAFolderPerObjectTypeAndOpens()
    {
        var database = Connect(new FakeSession());

        Assert.True(database.IsExpanded);
        Assert.Equal(ObjectFolderNode.Order, database.Children.OfType<ObjectFolderNode>().Select(f => f.Type));
        Assert.Equal("Tables", Folder(database, DatabaseObjectType.Table).Title);
    }

    [Fact]
    public void Folders_DoNotLoadUntilOpened()
    {
        var session = new FakeSession();
        var database = Connect(session);

        var tables = Folder(database, DatabaseObjectType.Table);

        Assert.Equal(0, session.ListCalls);
        Assert.IsType<MessageNode>(Assert.Single(tables.Children)); // "Loading…" placeholder
        Assert.Null(tables.Count);
    }

    [Fact]
    public async Task OpeningAFolder_LoadsItsObjects()
    {
        var session = new FakeSession().WithObjects(DatabaseObjectType.Table, "CUSTOMERS", "ORDERS");
        var tables = Folder(Connect(session), DatabaseObjectType.Table);

        await tables.LoadAsync();

        Assert.Equal(["CUSTOMERS", "ORDERS"], tables.Children.OfType<ObjectNode>().Select(n => n.Name));
        Assert.Equal(2, tables.Count);
        Assert.Equal("2", tables.CountText);
    }

    [Fact]
    public void Expanding_StartsTheLoad()
    {
        var session = new FakeSession().WithObjects(DatabaseObjectType.View, "V1");
        var views = Folder(Connect(session), DatabaseObjectType.View);

        views.IsExpanded = true;

        Assert.Equal(1, session.ListCalls);
        Assert.Equal("V1", Assert.IsType<ObjectNode>(Assert.Single(views.Children)).Name);
    }

    [Fact]
    public async Task EmptyFolder_SaysNone()
    {
        var roles = Folder(Connect(new FakeSession()), DatabaseObjectType.Role);

        await roles.LoadAsync();

        Assert.Equal("(none)", Assert.IsType<MessageNode>(Assert.Single(roles.Children)).Text);
        Assert.Equal(0, roles.Count);
    }

    [Fact]
    public async Task LoadFailure_ShowsTheErrorAndCanBeRetried()
    {
        var session = new FakeSession { ListFailure = new CoreException("connection lost", CoreErrorKind.Database) };
        var tables = Folder(Connect(session), DatabaseObjectType.Table);

        await tables.LoadAsync();

        var message = Assert.IsType<MessageNode>(Assert.Single(tables.Children));
        Assert.True(message.IsError);
        Assert.Equal("connection lost", message.Text);

        session.ListFailure = null;
        session.WithObjects(DatabaseObjectType.Table, "CUSTOMERS");
        await tables.LoadCommand.ExecuteAsync(null);

        Assert.Equal("CUSTOMERS", Assert.IsType<ObjectNode>(Assert.Single(tables.Children)).Name);
    }

    [Fact]
    public void Disconnecting_RemovesTheFolders()
    {
        var saved = TestData.Saved();
        _temp.Saved.Add(saved);
        var active = _connections.Add(new FakeSession(), saved.Id);
        var database = Assert.Single(_explorer.Databases);
        Assert.NotEmpty(database.Children);

        _connections.Disconnect(active);

        Assert.Empty(database.Children);
        Assert.False(database.IsExpanded);
    }

    [Fact]
    public void SelectingAnyNode_TargetsItsDatabase()
    {
        var database = Connect(new FakeSession());
        _explorer.SelectedDatabase = null;

        _explorer.SelectedNode = Folder(database, DatabaseObjectType.Table);

        Assert.Same(database, _explorer.SelectedDatabase);
        Assert.True(_explorer.DisconnectCommand.CanExecute(null));
    }

    [Fact]
    public void ObjectNode_ShowsParentFlagsAndInactiveState()
    {
        var info = new DatabaseObjectInfo
        {
            Type = DatabaseObjectType.Index,
            Name = "IX_NAME",
            Parent = "CUSTOMERS",
            Description = "Speeds up lookups",
            Flags = ["unique", "inactive"],
        };

        var node = new ObjectNode(Connect(new FakeSession()), info);

        Assert.True(node.HasParent);
        Assert.Equal("CUSTOMERS", node.Parent);
        Assert.Equal("(unique, inactive)", node.FlagsText);
        Assert.True(node.IsInactive);
        Assert.Equal("Speeds up lookups", node.Description);
    }
}

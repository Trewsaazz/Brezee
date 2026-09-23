using Brezee.App.Commands;
using Brezee.App.Connections;
using Brezee.App.Features.Explorer;
using Brezee.App.Features.Output;
using Brezee.App.Features.TableData;
using Brezee.App.Features.Welcome;
using Brezee.App.Shell;
using Brezee.App.Tests.Support;
using Brezee.Bridge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Shell;

// These tests also exercise the real C++/CLI bridge and native core (ShellViewModel reads CoreInfo.Version).
public sealed class ShellViewModelTests : IDisposable
{
    private readonly CommandRegistry _commands = new();
    private readonly FakeDialogService _dialogs = new();
    private readonly ConnectionManager _connections = new(NullLogger<ConnectionManager>.Instance);
    private readonly TempSavedConnections _temp = new();
    private readonly ExplorerViewModel _explorer;
    private readonly OutputViewModel _output = new();
    private readonly CapturingLoggerProvider _logs = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ShellViewModel _shell;

    public ShellViewModelTests()
    {
        _loggerFactory = _logs.CreateFactory();
        var coordinator = TestData.Coordinator(_dialogs, _connections, _temp.Saved);
        _explorer = new ExplorerViewModel(_connections, _temp.Saved, coordinator);
        _shell = new ShellViewModel(_commands, coordinator, _connections, _temp.CreateRecent(_connections), _explorer, _output,
            _loggerFactory.CreateLogger<ShellViewModel>());
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public void Startup_OpensWelcomePage()
    {
        var welcome = Assert.IsType<WelcomeViewModel>(Assert.Single(_shell.Documents));
        Assert.Same(welcome, _shell.ActiveDocument);
    }

    [Fact]
    public void Startup_ReadsVersionFromNativeCore()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+$", _shell.CoreVersion);
    }

    [Fact]
    public void Startup_HostsExplorerAndOutputPanels()
    {
        Assert.Equal(new ToolViewModel[] { _explorer, _output }, _shell.Tools);
    }

    [Fact]
    public void Startup_LogsCoreVersion()
    {
        var entry = Assert.Single(_logs.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains(_shell.CoreVersion, entry.Message);
    }

    [Theory]
    [InlineData("file.connect")]
    [InlineData("database.disconnect")]
    [InlineData("file.exit")]
    [InlineData("view.explorer")]
    [InlineData("view.output")]
    [InlineData("view.welcome")]
    public void Startup_RegistersShellCommands(string id)
    {
        Assert.Equal(id, _commands[id].Id);
    }

    [Fact]
    public void ClosingADocument_RemovesIt()
    {
        var welcome = _shell.Documents[0];

        welcome.CloseCommand.Execute(null);

        Assert.Empty(_shell.Documents);
    }

    [Fact]
    public void ViewWelcome_ReopensClosedWelcomePage()
    {
        var welcome = _shell.Documents[0];
        welcome.CloseCommand.Execute(null);

        _commands["view.welcome"].Execute(null);

        Assert.Same(welcome, Assert.Single(_shell.Documents));
        Assert.Same(welcome, _shell.ActiveDocument);
    }

    [Fact]
    public void ViewWelcome_WhenAlreadyOpen_DoesNotDuplicateIt()
    {
        _commands["view.welcome"].Execute(null);

        Assert.Single(_shell.Documents);
    }

    [Theory]
    [InlineData("view.explorer")]
    [InlineData("view.output")]
    public void ViewToolCommand_ShowsAndFocusesHiddenPanel(string id)
    {
        ToolViewModel tool = id == "view.explorer" ? _explorer : _output;
        tool.IsVisible = false;
        tool.IsActive = false;

        _commands[id].Execute(null);

        Assert.True(tool.IsVisible);
        Assert.True(tool.IsActive);
    }

    [Fact]
    public void Connect_WhenTheDialogSucceeds_AddsTheConnectionAndShowsTheExplorer()
    {
        var session = new FakeSession();
        _dialogs.ConnectResult = new ConnectResult(session, SavedAs: null);
        _explorer.IsVisible = false;

        _commands["file.connect"].Execute(null);

        Assert.Same(session, Assert.Single(_connections.Connections).Session);
        Assert.True(_explorer.IsVisible);
    }

    [Fact]
    public void Connect_WhenTheDialogIsCancelled_DoesNothing()
    {
        _commands["file.connect"].Execute(null);

        Assert.Single(_dialogs.ConnectDialogPrefills);
        Assert.Empty(_connections.Connections);
    }

    [Fact]
    public void Disconnect_ClosesTheSelectedDatabase()
    {
        var session = new FakeSession();
        _connections.Add(session);

        _commands["database.disconnect"].Execute(null);

        Assert.Empty(_connections.Connections);
        Assert.Equal(1, session.DisposeCount);
    }

    [Fact]
    public void Connections_AreAddedToRecent()
    {
        _connections.Add(new FakeSession("C:/data/employee.fdb"));

        Assert.Equal("employee.fdb", Assert.Single(_shell.Recent.Items).Name);
    }

    [Fact]
    public async Task OpenRecent_ReconnectsAndShowsTheExplorer()
    {
        var active = _connections.Add(new FakeSession("C:/data/employee.fdb"));
        _connections.Disconnect(active);
        _dialogs.ConnectResult = new ConnectResult(new FakeSession("C:/data/employee.fdb"), SavedAs: null);
        _explorer.IsVisible = false;

        await _shell.OpenRecentCommand.ExecuteAsync(_shell.Recent.Items[0]);

        Assert.Single(_connections.Connections);
        Assert.True(_explorer.IsVisible);
        Assert.Equal("C:/data/employee.fdb", _dialogs.ConnectDialogPrefills.Single()?.Database);
    }

    private ObjectNode ConnectWithTable(FakeSession session, string table)
    {
        _connections.Add(session);
        var database = _explorer.Databases.Single();
        return new ObjectNode(database, new DatabaseObjectInfo
        {
            Type = DatabaseObjectType.Table,
            Name = table,
            Parent = string.Empty,
            Description = string.Empty,
            Flags = [],
        });
    }

    [Fact]
    public void OpenData_OpensATabForTheTableAndReusesIt()
    {
        var node = ConnectWithTable(new FakeSession(), "CUSTOMERS");

        _explorer.OpenDataCommand.Execute(node);
        _explorer.OpenDataCommand.Execute(node);

        var document = Assert.Single(_shell.Documents.OfType<TableDataViewModel>());
        Assert.Equal("CUSTOMERS", document.Table);
        Assert.Same(document, _shell.ActiveDocument);
    }

    [Fact]
    public void Disconnecting_ClosesThatDatabasesTabs()
    {
        var node = ConnectWithTable(new FakeSession(), "CUSTOMERS");
        _explorer.OpenDataCommand.Execute(node);

        _connections.Disconnect(_connections.Connections.Single());

        Assert.Empty(_shell.Documents.OfType<TableDataViewModel>());
        Assert.Single(_shell.Documents); // The welcome page stays.
    }
}

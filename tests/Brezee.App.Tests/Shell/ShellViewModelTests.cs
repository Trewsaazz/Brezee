using Brezee.App.Commands;
using Brezee.App.Connections;
using Brezee.App.Features.Explorer;
using Brezee.App.Features.Output;
using Brezee.App.Features.Welcome;
using Brezee.App.Shell;
using Brezee.App.Tests.Support;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Shell;

// These tests also exercise the real C++/CLI bridge and native core (ShellViewModel reads CoreInfo.Version).
public sealed class ShellViewModelTests : IDisposable
{
    private readonly CommandRegistry _commands = new();
    private readonly FakeDialogService _dialogs = new();
    private readonly ConnectionManager _connections = new(NullLogger<ConnectionManager>.Instance);
    private readonly ExplorerViewModel _explorer;
    private readonly OutputViewModel _output = new();
    private readonly CapturingLoggerProvider _logs = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ShellViewModel _shell;

    public ShellViewModelTests()
    {
        _loggerFactory = _logs.CreateFactory();
        _explorer = new ExplorerViewModel(_connections);
        _shell = new ShellViewModel(_commands, _dialogs, _connections, _explorer, _output,
            _loggerFactory.CreateLogger<ShellViewModel>());
    }

    public void Dispose() => _loggerFactory.Dispose();

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
        _dialogs.ConnectResult = session;
        _explorer.IsVisible = false;

        _commands["file.connect"].Execute(null);

        Assert.Same(session, Assert.Single(_connections.Sessions));
        Assert.True(_explorer.IsVisible);
    }

    [Fact]
    public void Connect_WhenTheDialogIsCancelled_DoesNothing()
    {
        _commands["file.connect"].Execute(null);

        Assert.Equal(1, _dialogs.ConnectDialogShown);
        Assert.Empty(_connections.Sessions);
    }

    [Fact]
    public void Disconnect_ClosesTheSelectedDatabase()
    {
        var session = new FakeSession();
        _connections.Add(session);

        _commands["database.disconnect"].Execute(null);

        Assert.Empty(_connections.Sessions);
        Assert.Equal(1, session.DisposeCount);
    }
}

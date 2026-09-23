using Brezee.App.Commands;
using Brezee.App.Features.Explorer;
using Brezee.App.Features.Output;
using Brezee.App.Features.Welcome;
using Brezee.App.Shell;

namespace Brezee.App.Tests.Shell;

// These tests also exercise the real C++/CLI bridge and native core (ShellViewModel reads CoreInfo.Version).
public class ShellViewModelTests
{
    private readonly CommandRegistry _commands = new();
    private readonly ExplorerViewModel _explorer = new();
    private readonly OutputViewModel _output = new();
    private readonly ShellViewModel _shell;

    public ShellViewModelTests()
    {
        _shell = new ShellViewModel(_commands, _explorer, _output);
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
    public void Startup_LogsToOutputPanel()
    {
        var line = Assert.Single(_output.Lines);
        Assert.Contains(_shell.CoreVersion, line);
    }

    [Theory]
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
}

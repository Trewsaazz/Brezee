using Brezee.App.Features.Connect;
using Brezee.App.Tests.Support;
using Brezee.Bridge;

namespace Brezee.App.Tests.Features;

public class ConnectDialogViewModelTests
{
    private readonly FakeConnector _connector = new();
    private readonly ConnectDialogViewModel _dialog;

    public ConnectDialogViewModelTests()
    {
        _dialog = new ConnectDialogViewModel(_connector);
    }

    [Fact]
    public void Defaults_AreTheUsualFirebirdSettings()
    {
        Assert.Equal("localhost", _dialog.Host);
        Assert.Equal(3050, _dialog.Port);
        Assert.Equal("SYSDBA", _dialog.User);
        Assert.Equal("UTF8", _dialog.Charset);
    }

    [Fact]
    public void Connect_IsDisabledUntilADatabaseIsEntered()
    {
        Assert.False(_dialog.ConnectCommand.CanExecute(null));

        _dialog.Database = "employee";

        Assert.True(_dialog.ConnectCommand.CanExecute(null));
    }

    [Fact]
    public void ToSettings_TrimsFieldsButNotThePassword()
    {
        _dialog.Host = "  db.example.com ";
        _dialog.Port = 3051;
        _dialog.Database = " employee ";
        _dialog.Password = " secret ";

        var settings = _dialog.ToSettings();

        Assert.Equal("db.example.com", settings.Host);
        Assert.Equal(3051, settings.Port);
        Assert.Equal("employee", settings.Database);
        Assert.Equal(" secret ", settings.Password);
    }

    [Fact]
    public async Task Connect_Success_ExposesSessionAndRaisesConnected()
    {
        _dialog.Database = "employee";
        var connected = false;
        _dialog.Connected += (_, _) => connected = true;

        await _dialog.ConnectCommand.ExecuteAsync(null);

        Assert.True(connected);
        Assert.NotNull(_dialog.Session);
        Assert.Equal("employee", _connector.LastSettings?.Database);
        Assert.Null(_dialog.ErrorMessage);
        Assert.False(_dialog.IsBusy);
    }

    [Fact]
    public async Task Connect_Failure_ShowsFirebirdMessageAndStaysOpen()
    {
        _dialog.Database = "missing.fdb";
        _connector.Failure = new CoreException("I/O error during \"open\" operation", CoreErrorKind.Connection);
        var connected = false;
        _dialog.Connected += (_, _) => connected = true;

        await _dialog.ConnectCommand.ExecuteAsync(null);

        Assert.False(connected);
        Assert.Null(_dialog.Session);
        Assert.Equal("I/O error during \"open\" operation", _dialog.ErrorMessage);
        Assert.False(_dialog.IsBusy);
        Assert.True(_dialog.ConnectCommand.CanExecute(null)); // The user can fix the form and retry.
    }

    [Fact]
    public async Task Connect_Retry_ClearsThePreviousError()
    {
        _dialog.Database = "employee";
        _connector.Failure = new CoreException("Your user name and password are not defined", CoreErrorKind.Connection);
        await _dialog.ConnectCommand.ExecuteAsync(null);

        _connector.Failure = null;
        await _dialog.ConnectCommand.ExecuteAsync(null);

        Assert.Null(_dialog.ErrorMessage);
        Assert.NotNull(_dialog.Session);
    }
}

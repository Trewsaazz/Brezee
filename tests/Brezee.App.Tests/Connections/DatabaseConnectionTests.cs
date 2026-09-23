using Brezee.App.Tests.Diagnostics;
using Brezee.Bridge;

namespace Brezee.App.Tests.Connections;

// Runs through the real C++/CLI bridge and native core, with fbclient.dll from the Firebird kit.
// Shares the CoreRuntimeTests collection because the core's log sink and client are process-wide.
[Collection(nameof(CoreRuntimeTests))]
public sealed class DatabaseConnectionTests : IDisposable
{
    public DatabaseConnectionTests()
    {
        CoreRuntime.Initialize((_, _, _) => { });
    }

    public void Dispose() => CoreRuntime.Shutdown();

    [Fact]
    public void Open_UnreachableServer_ThrowsConnectionError()
    {
        // Nothing listens on port 1, so the attempt fails fast with a network error.
        var settings = new ConnectionSettings { Host = "127.0.0.1", Port = 1, Database = "employee", User = "SYSDBA" };

        var error = Assert.Throws<CoreException>(() => DatabaseConnection.Open(settings));

        Assert.Equal(CoreErrorKind.Connection, error.Kind);
        Assert.Contains("127.0.0.1", error.Message);
    }

    [Fact]
    public void Open_WithoutSettings_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DatabaseConnection.Open(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public void Open_InvalidPort_Throws(int port)
    {
        var settings = new ConnectionSettings { Host = "localhost", Port = port, Database = "employee" };

        Assert.Throws<ArgumentOutOfRangeException>(() => DatabaseConnection.Open(settings));
    }

    [Fact]
    public void ConnectionSettings_DefaultToStandardPortAndUtf8()
    {
        var settings = new ConnectionSettings();

        Assert.Equal(3050, settings.Port);
        Assert.Equal("UTF8", settings.Charset);
    }
}

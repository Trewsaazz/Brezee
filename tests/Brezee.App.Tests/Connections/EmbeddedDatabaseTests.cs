using System.IO;
using Brezee.App.Connections;
using Brezee.App.Tests.Diagnostics;
using Brezee.Bridge;

namespace Brezee.App.Tests.Connections;

// Opens real database files with the embedded engine that ships next to the app (copied into the
// test output with the same layout as Brezee.exe), so this also proves the shipped files are complete.
[Collection(nameof(CoreRuntimeTests))]
public sealed class EmbeddedDatabaseTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"brezee-app-test-{Guid.NewGuid():N}.fdb");

    public EmbeddedDatabaseTests()
    {
        CoreRuntime.Initialize((_, _, _) => { });
    }

    public void Dispose()
    {
        CoreRuntime.Shutdown();
        File.Delete(_path);
    }

    private ConnectionSettings Local => new() { Database = _path, User = "SYSDBA" };

    [Fact]
    public void CreateAndReadDetails()
    {
        using var connection = DatabaseConnection.Create(Local);

        var details = connection.GetDetails();

        Assert.True(connection.IsOpen);
        Assert.Contains("Firebird", details.ServerVersion);
        Assert.True(details.OdsMajor >= 13);
        Assert.Equal(3, details.SqlDialect);
    }

    [Fact]
    public async Task Connector_OpensAnExistingLocalDatabase()
    {
        DatabaseConnection.Create(Local).Dispose();

        using var session = await new FirebirdConnector().ConnectAsync(Local, TestContext.Current.CancellationToken);

        Assert.Equal(_path, session.Settings.Database);
        Assert.Contains("Firebird", session.Details.ServerVersion);
    }

    [Fact]
    public void Close_ThenDispose_IsSafe()
    {
        var connection = DatabaseConnection.Create(Local);

        connection.Close();
        Assert.False(connection.IsOpen);
        connection.Dispose();
        connection.Dispose();
    }

    [Fact]
    public void MissingFile_ThrowsConnectionErrorNamingTheFile()
    {
        var error = Assert.Throws<CoreException>(() => DatabaseConnection.Open(Local));

        Assert.Equal(CoreErrorKind.Connection, error.Kind);
        // Firebird normalizes Windows paths (8.3 short names, letter case), so compare the file name loosely.
        var fileName = Path.GetFileNameWithoutExtension(_path);
        Assert.True(error.Message.Contains(fileName, StringComparison.OrdinalIgnoreCase),
            $"Expected the error to name {fileName}. Message: {error.Message}");
    }
}

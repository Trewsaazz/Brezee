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

    [Fact]
    public void ListObjects_NewDatabase_HasNoUserObjectsButHasSystemTables()
    {
        using var connection = DatabaseConnection.Create(Local);

        Assert.Empty(connection.ListObjects(DatabaseObjectType.Table, includeSystem: false));

        var system = connection.ListObjects(DatabaseObjectType.Table, includeSystem: true);
        var relations = Assert.Single(system, o => o.Name == "RDB$RELATIONS");
        Assert.True(relations.IsSystem);
        Assert.Equal(DatabaseObjectType.Table, relations.Type);
    }

    [Fact]
    public void Execute_ReturnsTypedDotNetValues()
    {
        using var connection = DatabaseConnection.Create(Local);
        connection.Execute("create table t (id integer, amount numeric(10,2), born date, note varchar(10), photo blob)", [], 0);
        connection.Execute("insert into t values (?, ?, ?, ?, null)", ["7", "12.50", "1815-12-10", null], 0);

        var result = connection.Execute("select id, amount, born, note, photo from t", [], 0);

        Assert.Equal(["ID", "AMOUNT", "BORN", "NOTE", "PHOTO"], result.Columns.Select(c => c.Name));
        Assert.Equal(ResultColumnKind.Decimal, result.Columns[1].Kind);
        Assert.Equal("NUMERIC(10,2)", result.Columns[1].TypeName);
        var row = Assert.Single(result.Rows);
        Assert.Equal(7L, row[0]);
        Assert.Equal(12.50m, row[1]);
        Assert.Equal(new DateOnly(1815, 12, 10), row[2]);
        Assert.Null(row[3]);
        Assert.Null(row[4]);
    }

    [Fact]
    public void Execute_MaxRows_TruncatesAndSaysSo()
    {
        using var connection = DatabaseConnection.Create(Local);

        var result = connection.Execute("select rdb$type from rdb$types", [], 5);

        Assert.Equal(5, result.Rows.Count);
        Assert.True(result.Truncated);
    }

    [Fact]
    public void Execute_BadSql_ThrowsDatabaseError()
    {
        using var connection = DatabaseConnection.Create(Local);

        var error = Assert.Throws<CoreException>(() => connection.Execute("select * from nowhere", [], 0));

        Assert.Equal(CoreErrorKind.Database, error.Kind);
        Assert.Contains("NOWHERE", error.Message);
    }
}

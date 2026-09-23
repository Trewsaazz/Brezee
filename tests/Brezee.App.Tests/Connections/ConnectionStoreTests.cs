using System.IO;
using Brezee.App.Connections;
using Brezee.App.Tests.Support;

namespace Brezee.App.Tests.Connections;

public sealed class ConnectionStoreTests : IDisposable
{
    private readonly TempSavedConnections _temp = new();

    private ConnectionStore Store => _temp.Store;

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Load_WithoutFile_ReturnsNothing()
    {
        Assert.Empty(Store.Load());
    }

    [Fact]
    public void SaveThenLoad_RoundTripsEveryField()
    {
        var original = new SavedConnection
        {
            Name = "Sales (production)",
            IsLocal = false,
            Host = "db.example.com",
            Port = 3051,
            Database = @"D:\Firebird\Data\SALES.FDB",
            User = "REPORTER",
            Role = "READONLY",
            Charset = "WIN1252",
        };

        Store.Save([original]);
        var loaded = Assert.Single(Store.Load());

        Assert.Equal(original, loaded);
    }

    [Fact]
    public void Save_WithoutRememberedPassword_WritesNoPasswordField()
    {
        Store.Save([TestData.Saved()]);

        Assert.DoesNotContain("password\": \"", File.ReadAllText(Store.FilePath), StringComparison.OrdinalIgnoreCase);
        Assert.Null(Assert.Single(Store.Load()).ProtectedPassword);
    }

    [Fact]
    public void Save_KeepsOnlyTheProtectedFormOfARememberedPassword()
    {
        Store.Save([TestData.Saved() with { ProtectedPassword = new DpapiCredentialProtector().Protect("hunter2") }]);

        var json = File.ReadAllText(Store.FilePath);
        Assert.DoesNotContain("hunter2", json);
        Assert.DoesNotContain("hasSavedPassword", json);
        Assert.Equal("hunter2", new DpapiCredentialProtector().Unprotect(Assert.Single(Store.Load()).ProtectedPassword!));
    }

    [Fact]
    public void Save_ReplacesThePreviousContentAndLeavesNoTemporaryFile()
    {
        Store.Save([TestData.Saved("First")]);
        Store.Save([TestData.Saved("Second")]);

        Assert.Equal("Second", Assert.Single(Store.Load()).Name);
        Assert.False(File.Exists(Store.FilePath + ".tmp"));
    }

    [Fact]
    public void Load_DamagedFile_IsSetAsideAndTreatedAsEmpty()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Store.FilePath)!);
        File.WriteAllText(Store.FilePath, "{ this is not json");

        Assert.Empty(Store.Load());
        Assert.False(File.Exists(Store.FilePath));
        Assert.Equal("{ this is not json", File.ReadAllText(Store.FilePath + ".bad"));
    }

    [Fact]
    public void Load_ReadsTheFileFormatWrittenByEarlierVersions()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Store.FilePath)!);
        File.WriteAllText(Store.FilePath, """
            {
              "version": 1,
              "connections": [
                { "id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301", "name": "Employee", "isLocal": false,
                  "host": "localhost", "port": 3050, "database": "employee", "user": "SYSDBA",
                  "role": "", "charset": "UTF8" }
              ]
            }
            """);

        var loaded = Assert.Single(Store.Load());

        Assert.Equal("Employee", loaded.Name);
        Assert.Null(loaded.ProtectedPassword);
    }
}

using Brezee.App.Connections;
using Brezee.App.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Connections;

public sealed class SavedConnectionsTests : IDisposable
{
    private readonly TempSavedConnections _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Add_PersistsImmediatelyAndRaisesEvent()
    {
        var connection = TestData.Saved();
        SavedConnection? added = null;
        _temp.Saved.Added += (_, c) => added = c;

        _temp.Saved.Add(connection);

        Assert.Same(connection, added);
        Assert.Equal(connection, Assert.Single(_temp.Store.Load()));
    }

    [Fact]
    public void Remove_PersistsImmediatelyAndRaisesEvent()
    {
        var keep = TestData.Saved("Keep");
        var drop = TestData.Saved("Drop");
        _temp.Saved.Add(keep);
        _temp.Saved.Add(drop);
        SavedConnection? removed = null;
        _temp.Saved.Removed += (_, c) => removed = c;

        _temp.Saved.Remove(drop);

        Assert.Same(drop, removed);
        Assert.Equal("Keep", Assert.Single(_temp.Store.Load()).Name);
    }

    [Fact]
    public void SavedConnections_SurviveARestart()
    {
        _temp.Saved.Add(TestData.Saved("Employee"));

        var reloaded = new SavedConnections(_temp.Store, NullLogger<SavedConnections>.Instance);

        Assert.Equal("Employee", Assert.Single(reloaded.Items).Name);
    }

    [Fact]
    public void Find_ReturnsConnectionById()
    {
        var connection = TestData.Saved();
        _temp.Saved.Add(connection);

        Assert.Same(connection, _temp.Saved.Find(connection.Id));
        Assert.Null(_temp.Saved.Find(Guid.NewGuid()));
    }

    [Fact]
    public void ToSettings_UsesThePasswordGivenAndNoHostForLocalFiles()
    {
        var local = TestData.Saved(database: @"C:\Data\stock.fdb", host: "");

        var settings = local.ToSettings("secret");

        Assert.Equal(string.Empty, settings.Host);
        Assert.Equal(@"C:\Data\stock.fdb", settings.Database);
        Assert.Equal("secret", settings.Password);
    }
}

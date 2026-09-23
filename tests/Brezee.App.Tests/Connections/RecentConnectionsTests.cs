using System.IO;
using Brezee.App.Connections;
using Brezee.App.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brezee.App.Tests.Connections;

public sealed class RecentConnectionsTests : IDisposable
{
    private readonly TempSavedConnections _temp = new();
    private readonly ConnectionManager _connections = new(NullLogger<ConnectionManager>.Instance);
    private readonly RecentConnections _recent;

    public RecentConnectionsTests()
    {
        _recent = _temp.CreateRecent(_connections);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void EveryConnection_IsRecordedNewestFirst()
    {
        _connections.Add(new FakeSession("a.fdb"));
        _connections.Add(new FakeSession("b.fdb"));

        Assert.Equal(["b.fdb", "a.fdb"], _recent.Items.Select(r => r.Name));
    }

    [Fact]
    public void ReconnectingMovesTheEntryToTheTopWithoutDuplicating()
    {
        _connections.Add(new FakeSession("a.fdb"));
        _connections.Add(new FakeSession("b.fdb"));
        _connections.Add(new FakeSession("A.FDB")); // Same database, different spelling.

        Assert.Equal(["A.FDB", "b.fdb"], _recent.Items.Select(r => r.Name));
    }

    [Fact]
    public void KeepsOnlyTheLastTen()
    {
        for (var i = 1; i <= 12; i++)
            _connections.Add(new FakeSession($"db{i}.fdb"));

        Assert.Equal(RecentConnections.MaxEntries, _recent.Items.Count);
        Assert.Equal("db12.fdb", _recent.Items[0].Name);
        Assert.Equal("db3.fdb", _recent.Items[^1].Name);
    }

    [Fact]
    public void SavedConnection_IsRecordedUnderItsNameAndId()
    {
        var saved = TestData.Saved("Employee (prod)");
        _temp.Saved.Add(saved);

        _connections.Add(new FakeSession(saved.Database), saved.Id);

        var entry = Assert.Single(_recent.Items);
        Assert.Equal("Employee (prod)", entry.Name);
        Assert.Equal(saved.Id, entry.SavedConnectionId);
    }

    [Fact]
    public void LocalConnection_IsMarkedLocal()
    {
        _connections.Add(new FakeSession(@"C:\Data\stock.fdb", host: ""));

        var entry = Assert.Single(_recent.Items);
        Assert.True(entry.IsLocal);
        Assert.Equal("local", entry.Location);
    }

    [Fact]
    public void Recent_SurvivesARestartAndNeverStoresPasswords()
    {
        var session = new FakeSession("employee.fdb");
        session.Settings.Password = "hunter2";
        _connections.Add(session);

        var reloaded = _temp.CreateRecent(new ConnectionManager(NullLogger<ConnectionManager>.Instance));

        Assert.Equal("employee.fdb", Assert.Single(reloaded.Items).Name);
        Assert.DoesNotContain("hunter2", File.ReadAllText(_temp.RecentStore.FilePath));
    }
}

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;
using Brezee.App.Resources;
using Microsoft.Extensions.Logging;

namespace Brezee.App.Connections;

// A database connected to recently. Enough to connect again, never the password.
public sealed record RecentConnection
{
    public required string Name { get; init; }

    public bool IsLocal { get; init; }

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 3050;

    public required string Database { get; init; }

    public string User { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Charset { get; init; } = "UTF8";

    // Set when it was opened from a saved connection, so that one is used again (with its password).
    public Guid? SavedConnectionId { get; init; }

    public DateTimeOffset LastUsed { get; init; }

    // "server:port", or "local" for a file opened with the embedded engine.
    [JsonIgnore]
    public string Location => IsLocal
        ? Strings.Explorer_Local
        : string.Create(CultureInfo.InvariantCulture, $"{Host}:{Port}");

    // A template for the Connect dialog: the same details, as an unsaved connection.
    public SavedConnection ToTemplate() => new()
    {
        Name = Name,
        IsLocal = IsLocal,
        Host = Host,
        Port = Port,
        Database = Database,
        User = User,
        Role = Role,
        Charset = Charset,
    };

    // Two entries are the same database if they connect to the same place as the same user.
    public bool IsSameDatabaseAs(RecentConnection other) =>
        IsLocal == other.IsLocal
        && string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase)
        && Port == other.Port
        && string.Equals(Database, other.Database, StringComparison.OrdinalIgnoreCase)
        && string.Equals(User, other.User, StringComparison.OrdinalIgnoreCase);
}

// The recent connections file, %APPDATA%\Brezee\recent.json by default.
public sealed class RecentConnectionStore(string filePath, ILogger<RecentConnectionStore> logger)
    : JsonListFile<RecentConnection>(filePath, "recent", logger)
{
    public static string DefaultPath { get; } = DefaultPathFor("recent.json");
}

// The most recently used databases, newest first. Updated whenever a connection opens.
public sealed class RecentConnections
{
    public const int MaxEntries = 10;

    private readonly RecentConnectionStore _store;
    private readonly SavedConnections _saved;
    private readonly TimeProvider _time;

    public RecentConnections(RecentConnectionStore store, ConnectionManager connections, SavedConnections saved, TimeProvider time)
    {
        _store = store;
        _saved = saved;
        _time = time;

        foreach (var entry in store.Load().OrderByDescending(e => e.LastUsed).Take(MaxEntries))
            Items.Add(entry);

        connections.Added += (_, connection) => Record(connection);
    }

    public ObservableCollection<RecentConnection> Items { get; } = [];

    private void Record(ActiveConnection connection)
    {
        var settings = connection.Session.Settings;
        var saved = connection.SavedConnectionId is { } id ? _saved.Find(id) : null;

        var entry = new RecentConnection
        {
            Name = saved?.Name ?? DatabaseNames.FromPath(settings.Database),
            IsLocal = string.IsNullOrEmpty(settings.Host),
            Host = settings.Host ?? string.Empty,
            Port = settings.Port,
            Database = settings.Database ?? string.Empty,
            User = settings.User ?? string.Empty,
            Role = settings.Role ?? string.Empty,
            Charset = settings.Charset ?? string.Empty,
            SavedConnectionId = saved?.Id,
            LastUsed = _time.GetUtcNow(),
        };

        if (Items.FirstOrDefault(entry.IsSameDatabaseAs) is { } previous)
            Items.Remove(previous);

        Items.Insert(0, entry);
        while (Items.Count > MaxEntries)
            Items.RemoveAt(Items.Count - 1);

        _store.Save(Items);
    }
}

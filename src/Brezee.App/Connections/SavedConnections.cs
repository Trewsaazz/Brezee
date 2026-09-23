using Microsoft.Extensions.Logging;

namespace Brezee.App.Connections;

// The list of saved connections. Every change is written to disk straight away.
public sealed class SavedConnections
{
    private readonly ConnectionStore _store;
    private readonly ILogger<SavedConnections> _logger;
    private readonly List<SavedConnection> _items;

    public SavedConnections(ConnectionStore store, ILogger<SavedConnections> logger)
    {
        _store = store;
        _logger = logger;
        _items = store.Load().ToList();
    }

    public IReadOnlyList<SavedConnection> Items => _items;

    public event EventHandler<SavedConnection>? Added;

    public event EventHandler<SavedConnection>? Removed;

    public SavedConnection? Find(Guid id) => _items.FirstOrDefault(c => c.Id == id);

    public void Add(SavedConnection connection)
    {
        _items.Add(connection);
        Persist();
        _logger.LogInformation("Saved connection {Name}", connection.Name);
        Added?.Invoke(this, connection);
    }

    public void Remove(SavedConnection connection)
    {
        if (!_items.Remove(connection))
            return;

        Persist();
        _logger.LogInformation("Removed saved connection {Name}", connection.Name);
        Removed?.Invoke(this, connection);
    }

    private void Persist() => _store.Save(_items);
}

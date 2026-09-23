using Microsoft.Extensions.Logging;

namespace Brezee.App.Connections;

// The saved connections file, %APPDATA%\Brezee\connections.json by default.
public sealed class ConnectionStore(string filePath, ILogger<ConnectionStore> logger)
    : JsonListFile<SavedConnection>(filePath, "connections", logger)
{
    public static string DefaultPath { get; } = DefaultPathFor("connections.json");
}

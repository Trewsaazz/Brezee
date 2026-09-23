using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Brezee.App.Connections;

// Reads and writes the saved connections file, %APPDATA%\Brezee\connections.json by default.
public sealed class ConnectionStore(string filePath, ILogger<ConnectionStore> logger)
{
    private const int FormatVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Brezee", "connections.json");

    public string FilePath { get; } = filePath;

    // Returns the saved connections, or none if the file does not exist yet. A damaged file is set
    // aside as connections.json.bad (so nothing is silently lost) and treated as empty.
    public IReadOnlyList<SavedConnection> Load()
    {
        if (!File.Exists(FilePath))
            return [];

        try
        {
            using var stream = File.OpenRead(FilePath);
            var file = JsonSerializer.Deserialize<StoreFile>(stream, JsonOptions);
            return file?.Connections ?? [];
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            var backup = FilePath + ".bad";
            logger.LogWarning(ex, "Saved connections file is damaged; moved it to {Backup}", backup);
            File.Move(FilePath, backup, overwrite: true);
            return [];
        }
    }

    // Replaces the file atomically: a crash mid-save leaves the previous version intact.
    public void Save(IEnumerable<SavedConnection> connections)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        var temporary = FilePath + ".tmp";
        using (var stream = File.Create(temporary))
            JsonSerializer.Serialize(stream, new StoreFile(FormatVersion, connections.ToList()), JsonOptions);

        File.Move(temporary, FilePath, overwrite: true);
    }

    private sealed record StoreFile(int Version, List<SavedConnection> Connections);
}

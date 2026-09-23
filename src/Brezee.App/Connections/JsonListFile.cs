using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Brezee.App.Connections;

// A list of records kept in a JSON file under the user's profile:
// { "version": 1, "<listName>": [ ... ] }
public abstract class JsonListFile<T>(string filePath, string listName, ILogger logger)
{
    private const int FormatVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string FilePath { get; } = filePath;

    protected static string DefaultPathFor(string fileName) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Brezee", fileName);

    // Returns the stored items, or none if the file does not exist yet. A damaged file is set aside
    // as <name>.bad (so nothing is silently lost) and treated as empty.
    public IReadOnlyList<T> Load()
    {
        if (!File.Exists(FilePath))
            return [];

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(FilePath)) as JsonObject
                ?? throw new JsonException("Expected a JSON object.");
            return root[listName]?.Deserialize<List<T>>(JsonOptions) ?? [];
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            var backup = FilePath + ".bad";
            logger.LogWarning(ex, "{File} is damaged; moved it to {Backup}", FilePath, backup);
            File.Move(FilePath, backup, overwrite: true);
            return [];
        }
    }

    // Replaces the file atomically: a crash mid-save leaves the previous version intact.
    public void Save(IEnumerable<T> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        var root = new JsonObject
        {
            ["version"] = FormatVersion,
            [listName] = JsonSerializer.SerializeToNode(items.ToList(), JsonOptions),
        };

        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, root.ToJsonString(JsonOptions));
        File.Move(temporary, FilePath, overwrite: true);
    }
}

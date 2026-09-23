using Brezee.Bridge;

namespace Brezee.App.Features.Explorer;

// One database object in the tree.
public sealed class ObjectNode(DatabaseNodeViewModel database, DatabaseObjectInfo info) : ExplorerNode
{
    public override DatabaseNodeViewModel Database { get; } = database;

    public DatabaseObjectInfo Info { get; } = info;

    public string Name => Info.Name;

    // The owning table for triggers and indices, shown next to the name.
    public string Parent => Info.Parent;

    public bool HasParent => !string.IsNullOrEmpty(Info.Parent);

    public bool IsInactive => Info.Flags.Contains("inactive");

    // Flags such as "unique" or "legacy UDF", shown after the name.
    public string FlagsText => Info.Flags.Count == 0 ? string.Empty : $"({string.Join(", ", Info.Flags)})";

    // Tables and views have rows to show in a data grid.
    public bool HasData => Info.Type is DatabaseObjectType.Table or DatabaseObjectType.View;

    // Tooltip: the description, if the object has one.
    public string? Description => string.IsNullOrEmpty(Info.Description) ? null : Info.Description;
}

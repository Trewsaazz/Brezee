using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Brezee.App.Features.Explorer;

// A node in the explorer tree: a database, a folder of objects, an object, or a message.
public abstract partial class ExplorerNode : ObservableObject
{
    public ObservableCollection<ExplorerNode> Children { get; } = [];

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    // The database this node belongs to.
    public abstract DatabaseNodeViewModel Database { get; }

    partial void OnIsExpandedChanged(bool value)
    {
        if (value)
            OnExpanded();
    }

    // Called when the user opens the node; folders load their objects here.
    protected virtual void OnExpanded()
    {
    }
}

// A line of text in the tree: "Loading…", "(none)", or an error.
public sealed class MessageNode(DatabaseNodeViewModel database, string text, bool isError = false) : ExplorerNode
{
    public override DatabaseNodeViewModel Database { get; } = database;

    public string Text { get; } = text;

    public bool IsError { get; } = isError;
}

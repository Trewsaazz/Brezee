using System.IO;

namespace Brezee.App.Connections;

public static class DatabaseNames
{
    // A short display name for a database: the file name for a path ("employee.fdb"), or the
    // alias as is ("employee").
    public static string FromPath(string database)
    {
        var name = Path.GetFileName(database.TrimEnd('/', '\\'));
        return string.IsNullOrEmpty(name) ? database : name;
    }
}

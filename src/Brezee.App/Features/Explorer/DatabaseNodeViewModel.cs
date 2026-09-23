using System.Globalization;
using System.IO;
using Brezee.App.Connections;
using Brezee.App.Resources;

namespace Brezee.App.Features.Explorer;

// One open database in the explorer.
public sealed class DatabaseNodeViewModel(IDatabaseSession session)
{
    public IDatabaseSession Session { get; } = session;

    // The file name for a path ("employee.fdb"), or the alias as is ("employee").
    public string Name { get; } = DisplayName(session.Settings.Database);

    // Where the database lives: "server:port", or "local" for an embedded connection.
    public string Location { get; } = string.IsNullOrEmpty(session.Settings.Host)
        ? Strings.Explorer_Local
        : string.Create(CultureInfo.InvariantCulture, $"{session.Settings.Host}:{session.Settings.Port}");

    public string ServerVersion => Session.Details.ServerVersion;

    // Shown as a tooltip: the full path plus server details.
    public string Details => string.Format(CultureInfo.CurrentCulture, Strings.Explorer_DatabaseDetails,
        Session.Settings.Database, ServerVersion, Session.Details.OdsMajor, Session.Details.OdsMinor, Session.Details.PageSize);

    private static string DisplayName(string database)
    {
        var name = Path.GetFileName(database.TrimEnd('/', '\\'));
        return string.IsNullOrEmpty(name) ? database : name;
    }
}

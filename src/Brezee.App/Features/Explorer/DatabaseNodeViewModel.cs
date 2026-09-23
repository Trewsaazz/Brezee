using System.Globalization;
using Brezee.App.Connections;
using Brezee.App.Resources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Brezee.App.Features.Explorer;

// One database in the explorer: a saved connection, an open connection, or both.
public sealed partial class DatabaseNodeViewModel : ObservableObject
{
    public DatabaseNodeViewModel(SavedConnection? saved, ActiveConnection? active)
    {
        Saved = saved;
        Active = active;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name), nameof(Location), nameof(IsSaved), nameof(Details))]
    public partial SavedConnection? Saved { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name), nameof(Location), nameof(IsConnected), nameof(Status), nameof(Details))]
    public partial ActiveConnection? Active { get; set; }

    public bool IsSaved => Saved is not null;

    public bool IsConnected => Active is not null;

    // The saved name, or the database file name for an unsaved connection.
    public string Name => Saved?.Name ?? DatabaseNames.FromPath(Active?.Session.Settings.Database ?? string.Empty);

    // Where the database lives: "server:port", or "local" for a file opened with the embedded engine.
    public string Location
    {
        get
        {
            var (host, port) = Saved is { } saved
                ? (saved.IsLocal ? string.Empty : saved.Host, saved.Port)
                : (Active?.Session.Settings.Host ?? string.Empty, Active?.Session.Settings.Port ?? 0);

            return string.IsNullOrEmpty(host)
                ? Strings.Explorer_Local
                : string.Create(CultureInfo.InvariantCulture, $"{host}:{port}");
        }
    }

    // The server version when connected, otherwise "not connected".
    public string Status => Active?.Session.Details.ServerVersion ?? Strings.Explorer_NotConnected;

    // Tooltip: the full path plus server details when connected.
    public string Details
    {
        get
        {
            var database = Saved?.Database ?? Active?.Session.Settings.Database ?? string.Empty;
            if (Active is not { } active)
                return database;

            var details = active.Session.Details;
            return string.Format(CultureInfo.CurrentCulture, Strings.Explorer_DatabaseDetails,
                database, details.ServerVersion, details.OdsMajor, details.OdsMinor, details.PageSize);
        }
    }
}

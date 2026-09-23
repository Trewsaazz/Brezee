using System.Windows.Input;
using Brezee.App.Connections;
using Brezee.App.Resources;
using Brezee.App.Shell;

namespace Brezee.App.Features.Welcome;

// Start page shown when Brezee opens: a way to connect, and the recently used databases.
public sealed class WelcomeViewModel : DocumentViewModel
{
    public WelcomeViewModel(string coreVersion, RecentConnections recent, ICommand openRecent, ICommand connect)
        : base("welcome", Strings.Welcome_Title)
    {
        CoreVersion = coreVersion;
        Recent = recent;
        OpenRecentCommand = openRecent;
        ConnectCommand = connect;
    }

    public string CoreVersion { get; }

    public RecentConnections Recent { get; }

    // Takes the RecentConnection to open as parameter.
    public ICommand OpenRecentCommand { get; }

    public ICommand ConnectCommand { get; }
}

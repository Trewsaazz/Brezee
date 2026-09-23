using Brezee.App.Connections;

namespace Brezee.App.Shell;

// What the Connect dialog produced: the open session and, if the user asked, a new saved connection.
public sealed record ConnectResult(IDatabaseSession Session, SavedConnection? SavedAs);

// Opens dialogs on behalf of view models, which must not reference windows directly.
public interface IDialogService
{
    // Shows the Connect dialog, pre-filled from a saved connection if given.
    // Returns null if the user cancelled.
    ConnectResult? ShowConnectDialog(SavedConnection? prefill);
}

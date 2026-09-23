using Brezee.App.Connections;

namespace Brezee.App.Shell;

// What the Connect dialog produced: the open session and, if anything should be remembered, the
// saved connection to add (new) or update (same Id as the one the dialog was opened for).
public sealed record ConnectResult(IDatabaseSession Session, SavedConnection? SavedAs);

// Opens dialogs on behalf of view models, which must not reference windows directly.
public interface IDialogService
{
    // Shows the Connect dialog, optionally showing why an earlier attempt failed. With a prefill, the
    // form starts with its details: as that saved connection (prefillIsSaved), or as a template for a
    // new one. Returns null if the user cancelled.
    ConnectResult? ShowConnectDialog(SavedConnection? prefill, string? error = null, bool prefillIsSaved = true);
}

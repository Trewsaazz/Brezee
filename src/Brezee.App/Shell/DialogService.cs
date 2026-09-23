using System.Windows;
using Brezee.App.Connections;
using Brezee.App.Features.Connect;

namespace Brezee.App.Shell;

public sealed class DialogService(IDatabaseConnector connector, ICredentialProtector protector) : IDialogService
{
    public ConnectResult? ShowConnectDialog(SavedConnection? prefill, string? error = null, bool prefillIsSaved = true)
    {
        var viewModel = new ConnectDialogViewModel(connector, protector);
        if (prefill is not null)
            viewModel.LoadFrom(prefill, prefillIsSaved);
        viewModel.ErrorMessage = error;

        var dialog = new ConnectDialog(viewModel) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true || viewModel.Session is null)
            return null;

        return new ConnectResult(viewModel.Session, viewModel.CreateSavedConnection());
    }
}

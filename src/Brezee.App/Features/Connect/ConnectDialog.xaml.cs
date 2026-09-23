using System.Windows;
using System.Windows.Controls;
using Brezee.App.Resources;
using Microsoft.Win32;

namespace Brezee.App.Features.Connect;

public partial class ConnectDialog : Window
{
    private readonly ConnectDialogViewModel _viewModel;

    public ConnectDialog(ConnectDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        _viewModel.Connected += (_, _) => DialogResult = true;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // A connect attempt cannot be cancelled once started; closing now would leak its connection.
        if (_viewModel.IsBusy)
            e.Cancel = true;

        base.OnClosing(e);
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Strings.Connect_BrowseTitle,
            Filter = Strings.Connect_BrowseFilter,
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) == true)
            _viewModel.Database = dialog.FileName;
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Password = ((PasswordBox)sender).Password;
}

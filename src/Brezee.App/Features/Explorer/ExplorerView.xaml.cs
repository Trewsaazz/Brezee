using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Brezee.App.Features.Explorer;

public partial class ExplorerView : UserControl
{
    public ExplorerView()
    {
        InitializeComponent();
        Tree.PreviewKeyDown += OnPreviewKeyDown;
    }

    private ExplorerViewModel? ViewModel => DataContext as ExplorerViewModel;

    // TreeView.SelectedItem cannot be bound, so the selection is passed on here.
    private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (ViewModel is { } viewModel)
            viewModel.SelectedNode = e.NewValue as ExplorerNode;
    }

    // Double-click: connects a disconnected database, opens a table's or view's data.
    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Activate(Tree.SelectedItem))
            e.Handled = true;
    }

    // Enter does the same as double-click (elsewhere it keeps its usual meaning).
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Activate(Tree.SelectedItem))
            e.Handled = true;
    }

    private bool Activate(object? item)
    {
        if (ViewModel is not { } viewModel)
            return false;

        switch (item)
        {
            case DatabaseNodeViewModel { IsConnected: false } database when viewModel.ConnectCommand.CanExecute(database):
                viewModel.ConnectCommand.Execute(database);
                return true;
            case ObjectNode { HasData: true } node:
                viewModel.OpenDataCommand.Execute(node);
                return true;
            default:
                return false;
        }
    }
}

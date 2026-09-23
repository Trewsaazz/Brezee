using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Brezee.Bridge;

namespace Brezee.App.Features.TableData;

public partial class TableDataView : UserControl
{
    private TableDataViewModel? _viewModel;

    public TableDataView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = e.NewValue as TableDataViewModel;
        if (_viewModel is null)
            return;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        BuildColumns();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TableDataViewModel.Columns))
            BuildColumns();
        else if (e.PropertyName is nameof(TableDataViewModel.SortColumn) or nameof(TableDataViewModel.SortDescending))
            ShowSortGlyph();
    }

    // One text column per result column, bound by position: rows are object?[] arrays.
    private void BuildColumns()
    {
        Grid.Columns.Clear();
        if (_viewModel is null)
            return;

        for (var i = 0; i < _viewModel.Columns.Count; i++)
        {
            var column = _viewModel.Columns[i];
            var path = $"[{i}]";

            Grid.Columns.Add(new DataGridTextColumn
            {
                Header = new TextBlock { Text = column.Name, ToolTip = ColumnToolTip(column) },
                SortMemberPath = column.Name,
                Binding = new Binding(path) { Converter = DisplayConverter.Instance, Mode = BindingMode.OneWay },
                ElementStyle = CellStyle(path, IsNumeric(column.Kind)),
            });
        }

        ShowSortGlyph();
    }

    private void ShowSortGlyph()
    {
        if (_viewModel is null)
            return;

        foreach (var column in Grid.Columns)
        {
            column.SortDirection = column.SortMemberPath == _viewModel.SortColumn
                ? (_viewModel.SortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending)
                : null;
        }
    }

    // Sorting happens on the server, so every row is in order, not just the loaded ones.
    private void OnSorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;
        if (_viewModel?.SortByCommand.CanExecute(e.Column.SortMemberPath) == true)
            _viewModel.SortByCommand.Execute(e.Column.SortMemberPath);
    }

    private static string ColumnToolTip(ResultColumn column) =>
        column.IsNullable ? column.TypeName : $"{column.TypeName} NOT NULL";

    private static bool IsNumeric(ResultColumnKind kind) =>
        kind is ResultColumnKind.Integer or ResultColumnKind.Decimal or ResultColumnKind.Float;

    // Numbers right-aligned; NULL shown grey and italic.
    private static Style CellStyle(string path, bool numeric)
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(4, 0, 4, 0)));
        if (numeric)
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));

        var isNull = new DataTrigger { Binding = new Binding(path), Value = null };
        isNull.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Gray));
        isNull.Setters.Add(new Setter(TextBlock.FontStyleProperty, FontStyles.Italic));
        style.Triggers.Add(isNull);
        return style;
    }

    private sealed class DisplayConverter : IValueConverter
    {
        public static DisplayConverter Instance { get; } = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            ValueFormatter.Format(value);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}

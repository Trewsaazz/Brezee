using System.Collections.ObjectModel;
using System.Globalization;
using Brezee.App.Connections;
using Brezee.App.Resources;
using Brezee.App.Shell;
using Brezee.Bridge;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Brezee.App.Features.TableData;

// A document showing the rows of a table or view, a page at a time, sorted and filtered on the server.
public sealed partial class TableDataViewModel : DocumentViewModel
{
    public const int PageSize = 200;

    public TableDataViewModel(ActiveConnection connection, string databaseName, string table)
        : base(ContentIdFor(connection, table), $"{table} ({databaseName})")
    {
        Connection = connection;
        Table = table;
        Filter = string.Empty;
        AppliedFilter = string.Empty;
    }

    public ActiveConnection Connection { get; }

    public string Table { get; }

    // Column descriptions from the last load; the view builds its grid columns from these.
    [ObservableProperty]
    public partial IReadOnlyList<ResultColumn> Columns { get; private set; } = [];

    public ObservableCollection<object?[]> Rows { get; } = [];

    // WHERE condition being typed, e.g. CITY = 'Madrid'. Applied with ApplyFilter.
    [ObservableProperty]
    public partial string Filter { get; set; }

    // The condition the current rows were loaded with.
    [ObservableProperty]
    public partial string AppliedFilter { get; private set; }

    [ObservableProperty]
    public partial string? SortColumn { get; private set; }

    [ObservableProperty]
    public partial bool SortDescending { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand), nameof(RefreshCommand), nameof(ApplyFilterCommand), nameof(SortByCommand))]
    public partial bool IsLoading { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    public partial bool HasMoreRows { get; private set; }

    // Why the last load failed, in Firebird's words.
    [ObservableProperty]
    public partial string? ErrorMessage { get; private set; }

    // "200 rows (more available)" or "57 rows".
    public string StatusText => string.Format(CultureInfo.CurrentCulture,
        HasMoreRows ? Strings.TableData_RowsMore : Strings.TableData_Rows, Rows.Count);

    public static string ContentIdFor(ActiveConnection connection, string table) =>
        $"table:{connection.Id:N}:{table}";

    // Loads the first page. Called when the document opens.
    public Task LoadAsync() => LoadPageAsync(reset: true);

    [RelayCommand(CanExecute = nameof(CanReload))]
    private Task RefreshAsync() => LoadPageAsync(reset: true);

    [RelayCommand(CanExecute = nameof(CanReload))]
    private Task ApplyFilterAsync()
    {
        AppliedFilter = Filter.Trim();
        return LoadPageAsync(reset: true);
    }

    [RelayCommand(CanExecute = nameof(CanLoadMore))]
    private Task LoadMoreAsync() => LoadPageAsync(reset: false);

    // Sorts by a column: ascending first, descending when clicked again.
    [RelayCommand(CanExecute = nameof(CanSort))]
    private Task SortByAsync(string column)
    {
        SortDescending = SortColumn == column && !SortDescending;
        SortColumn = column;
        return LoadPageAsync(reset: true);
    }

    private bool CanReload() => !IsLoading;

    private bool CanSort(string column) => !IsLoading;

    private bool CanLoadMore() => !IsLoading && HasMoreRows;

    private async Task LoadPageAsync(bool reset)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var skip = reset ? 0 : Rows.Count;
            var sql = TableQuery.Build(Table, AppliedFilter, SortColumn, SortDescending, skip, PageSize);
            var result = await Connection.Session.ExecuteAsync(sql, maxRows: PageSize);

            if (reset)
            {
                Rows.Clear();
                // Keep the grid's columns (and their widths) unless the table itself changed.
                if (!Columns.Select(c => c.Name).SequenceEqual(result.Columns.Select(c => c.Name)))
                    Columns = result.Columns;
            }

            foreach (var row in result.Rows)
                Rows.Add(row);
            HasMoreRows = result.Truncated;
        }
        catch (CoreException ex)
        {
            // E.g. a typo in the filter: show it above the grid and keep the old rows.
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(StatusText));
        }
    }
}

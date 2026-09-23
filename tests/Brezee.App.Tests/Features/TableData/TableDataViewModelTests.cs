using System.Text.RegularExpressions;
using Brezee.App.Connections;
using Brezee.App.Features.TableData;
using Brezee.App.Tests.Support;
using Brezee.Bridge;

namespace Brezee.App.Tests.Features.TableData;

public class TableDataViewModelTests
{
    private readonly FakeSession _session = new();
    private readonly TableDataViewModel _table;

    public TableDataViewModelTests()
    {
        _table = new TableDataViewModel(new ActiveConnection(_session, null), "employee.fdb", "CUSTOMERS");
    }

    // Serves `total` rows of (ID) through the ROWS clause, like Firebird would.
    private void ServeRows(int total) => _session.Query = (sql, maxRows) =>
    {
        var match = Regex.Match(sql, @"rows (\d+) to (\d+)");
        var first = int.Parse(match.Groups[1].Value);
        var last = Math.Min(int.Parse(match.Groups[2].Value), total);
        var rows = Enumerable.Range(first, Math.Max(0, last - first + 1)).Select(i => new object?[] { (long)i }).ToList();
        return new QueryResultData
        {
            Columns = [new ResultColumn { Name = "ID", TypeName = "INTEGER", Kind = ResultColumnKind.Integer }],
            Rows = rows.Take(maxRows).ToList(),
            Truncated = rows.Count > maxRows,
        };
    };

    [Fact]
    public void Title_NamesTheTableAndDatabase()
    {
        Assert.Equal("CUSTOMERS (employee.fdb)", _table.Title);
    }

    [Fact]
    public async Task Load_ShowsTheFirstPageAndSaysIfThereIsMore()
    {
        ServeRows(450);

        await _table.LoadAsync();

        Assert.Equal(TableDataViewModel.PageSize, _table.Rows.Count);
        Assert.True(_table.HasMoreRows);
        Assert.True(_table.LoadMoreCommand.CanExecute(null));
        Assert.Equal("ID", Assert.Single(_table.Columns).Name);
        Assert.Contains("more", _table.StatusText);
    }

    [Fact]
    public async Task LoadMore_AppendsTheNextPageUntilAllRowsAreLoaded()
    {
        ServeRows(450);
        await _table.LoadAsync();

        await _table.LoadMoreCommand.ExecuteAsync(null);
        await _table.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal(450, _table.Rows.Count);
        Assert.Equal(450L, _table.Rows[^1][0]);
        Assert.False(_table.HasMoreRows);
        Assert.False(_table.LoadMoreCommand.CanExecute(null));
        Assert.Equal("450 rows", _table.StatusText);
    }

    [Fact]
    public async Task SortBy_SortsOnTheServerAndTogglesDirection()
    {
        ServeRows(10);
        await _table.LoadAsync();

        await _table.SortByCommand.ExecuteAsync("NAME");
        Assert.Contains("order by \"NAME\" asc", _session.ExecutedSql[^1]);

        await _table.SortByCommand.ExecuteAsync("NAME");
        Assert.Contains("order by \"NAME\" desc", _session.ExecutedSql[^1]);
        Assert.True(_table.SortDescending);

        await _table.SortByCommand.ExecuteAsync("ID");
        Assert.Contains("order by \"ID\" asc", _session.ExecutedSql[^1]);
    }

    [Fact]
    public async Task ApplyFilter_ReloadsWithTheCondition()
    {
        ServeRows(10);
        await _table.LoadAsync();
        _table.Filter = "ID > 5";

        await _table.ApplyFilterCommand.ExecuteAsync(null);

        Assert.Contains("where ID > 5", _session.ExecutedSql[^1]);
        Assert.Equal("ID > 5", _table.AppliedFilter);
    }

    [Fact]
    public async Task BadFilter_ShowsFirebirdsErrorAndKeepsTheRows()
    {
        ServeRows(10);
        await _table.LoadAsync();
        _session.QueryFailure = new CoreException("Column unknown: NOPE", CoreErrorKind.Database);
        _table.Filter = "NOPE = 1";

        await _table.ApplyFilterCommand.ExecuteAsync(null);

        Assert.Equal("Column unknown: NOPE", _table.ErrorMessage);
        Assert.Equal(10, _table.Rows.Count);
        Assert.False(_table.IsLoading);

        _session.QueryFailure = null;
        await _table.RefreshCommand.ExecuteAsync(null);
        Assert.Null(_table.ErrorMessage);
    }

    [Fact]
    public async Task Reloading_KeepsTheSameColumnsObject()
    {
        ServeRows(10);
        await _table.LoadAsync();
        var columns = _table.Columns;

        await _table.RefreshCommand.ExecuteAsync(null);

        Assert.Same(columns, _table.Columns); // So the grid keeps its column widths.
    }
}

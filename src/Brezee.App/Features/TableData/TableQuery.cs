using System.Globalization;
using System.Text;

namespace Brezee.App.Features.TableData;

// Builds the SELECT that the table data grid runs for one page.
public static class TableQuery
{
    // Quotes an identifier exactly as stored in the system tables, so mixed-case and reserved
    // names work: CUSTOMERS -> "CUSTOMERS", My "odd" name -> "My ""odd"" name".
    public static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    // SELECT * from the table, optionally filtered by a WHERE condition the user typed, sorted by
    // one column, returning rows [skip+1, skip+take+1]: one more than asked for, so the caller can
    // tell whether another page exists (run it with maxRows = take).
    public static string Build(string table, string? filter, string? sortColumn, bool descending, int skip, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);

        var sql = new StringBuilder("select * from ").Append(Quote(table));

        if (!string.IsNullOrWhiteSpace(filter))
            sql.Append(" where ").Append(filter.Trim());

        if (!string.IsNullOrEmpty(sortColumn))
            sql.Append(" order by ").Append(Quote(sortColumn)).Append(descending ? " desc" : " asc");

        sql.Append(CultureInfo.InvariantCulture, $" rows {skip + 1} to {skip + take + 1}");
        return sql.ToString();
    }
}

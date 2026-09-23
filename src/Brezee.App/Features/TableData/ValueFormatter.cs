using System.Globalization;

namespace Brezee.App.Features.TableData;

// How database values are shown in grids. Dates and numbers use invariant, unambiguous formats
// (2026-09-23, 1234.5) so what you see is what the database holds.
public static class ValueFormatter
{
    public const string NullText = "<null>";

    // Binary values show at most this many bytes before "…".
    public const int MaxBinaryBytes = 32;

    public static string Format(object? value) => value switch
    {
        null => NullText,
        bool b => b ? "true" : "false",
        long l => l.ToString(CultureInfo.InvariantCulture),
        decimal d => d.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly t => t.Ticks % TimeSpan.TicksPerSecond == 0
            ? t.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
            : t.ToString("HH:mm:ss.ffff", CultureInfo.InvariantCulture),
        DateTime dt => dt.Ticks % TimeSpan.TicksPerSecond == 0
            ? dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : dt.ToString("yyyy-MM-dd HH:mm:ss.ffff", CultureInfo.InvariantCulture),
        byte[] bytes => FormatBytes(bytes),
        string s => s,
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    private static string FormatBytes(byte[] bytes)
    {
        var shown = Convert.ToHexString(bytes, 0, Math.Min(bytes.Length, MaxBinaryBytes));
        return bytes.Length > MaxBinaryBytes
            ? string.Create(CultureInfo.InvariantCulture, $"0x{shown}… ({bytes.Length:N0} bytes)")
            : $"0x{shown}";
    }
}

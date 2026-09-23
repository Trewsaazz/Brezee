using Brezee.App.Features.TableData;

namespace Brezee.App.Tests.Features.TableData;

public class ValueFormatterTests
{
    public static TheoryData<object?, string> Values => new()
    {
        { null, "<null>" },
        { true, "true" },
        { 42L, "42" },
        { -1234.56m, "-1234.56" },
        { 1.5d, "1.5" },
        { "text", "text" },
        { "170141183460469231731687303715884105727", "170141183460469231731687303715884105727" },
        { new DateOnly(2026, 9, 23), "2026-09-23" },
        { new TimeOnly(13, 45, 30), "13:45:30" },
        { new TimeOnly(13, 45, 30).Add(TimeSpan.FromTicks(1234 * 1000)), "13:45:30.1234" },
        { new DateTime(2026, 9, 23, 13, 45, 30), "2026-09-23 13:45:30" },
        { new byte[] { 0xCA, 0xFE }, "0xCAFE" },
    };

    [Theory]
    [MemberData(nameof(Values))]
    public void Format_UsesUnambiguousInvariantFormats(object? value, string expected)
    {
        Assert.Equal(expected, ValueFormatter.Format(value));
    }

    [Fact]
    public void Format_LongBinary_IsShortenedWithItsSize()
    {
        var text = ValueFormatter.Format(new byte[1000]);

        Assert.StartsWith("0x" + new string('0', ValueFormatter.MaxBinaryBytes * 2) + "…", text);
        Assert.EndsWith("(1,000 bytes)", text);
    }
}

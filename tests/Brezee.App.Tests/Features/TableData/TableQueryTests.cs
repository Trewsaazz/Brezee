using Brezee.App.Features.TableData;

namespace Brezee.App.Tests.Features.TableData;

public class TableQueryTests
{
    [Theory]
    [InlineData("CUSTOMERS", "\"CUSTOMERS\"")]
    [InlineData("Mixed Case", "\"Mixed Case\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    public void Quote_EscapesIdentifiers(string identifier, string expected)
    {
        Assert.Equal(expected, TableQuery.Quote(identifier));
    }

    [Fact]
    public void Build_FirstPage_FetchesOneExtraRow()
    {
        Assert.Equal("select * from \"CUSTOMERS\" rows 1 to 201",
            TableQuery.Build("CUSTOMERS", filter: null, sortColumn: null, descending: false, skip: 0, take: 200));
    }

    [Fact]
    public void Build_NextPage_StartsAfterTheLoadedRows()
    {
        Assert.Equal("select * from \"T\" rows 201 to 401",
            TableQuery.Build("T", null, null, false, skip: 200, take: 200));
    }

    [Fact]
    public void Build_WithFilterAndSort()
    {
        Assert.Equal("select * from \"CUSTOMERS\" where CITY = 'Madrid' order by \"NAME\" desc rows 1 to 11",
            TableQuery.Build("CUSTOMERS", "  CITY = 'Madrid' ", "NAME", descending: true, skip: 0, take: 10));
    }

    [Fact]
    public void Build_BlankFilterIsIgnored()
    {
        Assert.DoesNotContain("where", TableQuery.Build("T", "   ", null, false, 0, 10));
    }

    [Fact]
    public void Build_RejectsInvalidPaging()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TableQuery.Build("T", null, null, false, -1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => TableQuery.Build("T", null, null, false, 0, 0));
    }
}

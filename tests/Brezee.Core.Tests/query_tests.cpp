#include <brezee/core/connection.h>
#include <brezee/core/error.h>
#include <brezee/core/query.h>

#include "test_support.h"

#include <doctest.h>

#include <memory>

using namespace brezee::core;

namespace {

// A fresh embedded database for one test.
struct TestDatabase
{
    test_support::TempDatabase file;
    std::unique_ptr<Connection> connection;

    TestDatabase()
    {
        test_support::initialize_core();
        ConnectionParameters parameters;
        parameters.database = file.path();
        parameters.user = "SYSDBA";
        connection = Connection::create(parameters);
    }
};

template <typename T>
const T& as(const Value& value)
{
    REQUIRE_MESSAGE(std::holds_alternative<T>(value), "value holds a different type (index ", value.index(), ")");
    return std::get<T>(value);
}

} // namespace

TEST_CASE("execute runs DDL and DML, then SELECT returns the rows")
{
    TestDatabase db;
    auto& c = *db.connection;

    const auto ddl = c.execute("create table people (id integer not null primary key, name varchar(40))");
    CHECK(ddl.columns.empty());
    c.execute("insert into people values (1, 'Ada')");
    c.execute("insert into people values (2, 'Grace')");

    const auto result = c.execute("select id, name from people order by id");

    REQUIRE(result.columns.size() == 2);
    CHECK(result.columns[0].name == "ID");
    CHECK(result.columns[0].relation == "PEOPLE");
    CHECK(result.columns[0].kind == ColumnKind::Integer);
    CHECK_FALSE(result.columns[0].nullable);
    CHECK(result.columns[1].type == "VARCHAR(40)");
    CHECK(result.columns[1].kind == ColumnKind::Text);

    REQUIRE(result.rows.size() == 2);
    CHECK(as<std::int64_t>(result.rows[0][0]) == 1);
    CHECK(as<std::string>(result.rows[0][1]) == "Ada");
    CHECK(as<std::string>(result.rows[1][1]) == "Grace");
    CHECK_FALSE(result.truncated);
}

TEST_CASE("every supported type is read exactly")
{
    TestDatabase db;
    auto& c = *db.connection;

    const auto result = c.execute(R"(
        select
            true, cast(-7 as smallint), cast(2147483647 as integer), cast(-9223372036854775808 as bigint),
            cast(1234.56 as numeric(6,2)), cast(-0.05 as numeric(18,4)), cast(170141183460469231731687303715884105727 as int128),
            cast('12.345678901234567890' as decfloat(34)),
            cast(1.5 as double precision), cast(0.25 as float),
            cast('pädded' as char(10)), cast('日本語 ✓' as varchar(20)), cast(x'CAFE' as varchar(2) character set octets),
            date '2026-09-23', time '13:45:30.1234', timestamp '2026-09-23 13:45:30.5',
            cast('2026-09-23 13:45:30 Europe/Madrid' as timestamp with time zone),
            cast('long text blob' as blob sub_type text), cast(x'0001FF' as blob sub_type binary),
            cast(null as integer)
        from rdb$database
    )");

    REQUIRE(result.rows.size() == 1);
    const auto& row = result.rows[0];

    CHECK(as<bool>(row[0]) == true);
    CHECK(as<std::int64_t>(row[1]) == -7);
    CHECK(as<std::int64_t>(row[2]) == 2147483647);
    CHECK(as<std::int64_t>(row[3]) == INT64_MIN);
    CHECK(as<Decimal>(row[4]).text == "1234.56");
    CHECK(as<Decimal>(row[5]).text == "-0.0500");
    CHECK(as<Decimal>(row[6]).text == "170141183460469231731687303715884105727");
    CHECK(as<Decimal>(row[7]).text == "12.345678901234567890");
    CHECK(as<double>(row[8]) == 1.5);
    CHECK(as<double>(row[9]) == 0.25);
    CHECK(as<std::string>(row[10]) == "pädded"); // CHAR padding removed
    CHECK(as<std::string>(row[11]) == "日本語 ✓");
    CHECK(as<Bytes>(row[12]) == Bytes{0xCA, 0xFE});
    CHECK(as<Date>(row[13]) == Date{2026, 9, 23});
    CHECK(as<Time>(row[14]) == Time{13, 45, 30, 1234});
    CHECK(as<Timestamp>(row[15]) == Timestamp{{2026, 9, 23}, {13, 45, 30, 5000}});
    CHECK(as<std::string>(row[16]).find("Europe/Madrid") != std::string::npos);
    CHECK(as<std::string>(row[17]) == "long text blob");
    CHECK(as<Bytes>(row[18]) == Bytes{0x00, 0x01, 0xFF});
    CHECK(is_null(row[19]));

    CHECK(result.columns[4].type == "NUMERIC(9,2)");
    CHECK(result.columns[4].kind == ColumnKind::Decimal);
    CHECK(result.columns[6].kind == ColumnKind::Decimal);
    CHECK(result.columns[12].kind == ColumnKind::Binary);
    CHECK(result.columns[16].type == "TIMESTAMP WITH TIME ZONE");
    CHECK(result.columns[17].kind == ColumnKind::Text);
    CHECK(result.columns[18].kind == ColumnKind::Binary);
}

TEST_CASE("table columns report their declared precision")
{
    TestDatabase db;
    auto& c = *db.connection;
    c.execute("create domain d_price as decimal(7,3)");
    c.execute("create table prices (amount numeric(10,2), unit_price d_price, total numeric(18,4))");

    const auto result = c.execute("select amount, unit_price, total, amount * 2 as doubled from prices");

    CHECK(result.columns[0].type == "NUMERIC(10,2)");
    CHECK(result.columns[1].type == "DECIMAL(7,3)");
    CHECK(result.columns[2].type == "NUMERIC(18,4)");
    // An expression has no declared type; its storage precision is the best available (Firebird 4+
    // computes it in 128 bits, so NUMERIC(38,2)).
    CHECK(result.columns[3].relation.empty());
    CHECK(result.columns[3].type.rfind("NUMERIC(", 0) == 0);
    CHECK(result.columns[3].type != "NUMERIC(10,2)");
}

TEST_CASE("parameters are passed as text and converted by Firebird")
{
    TestDatabase db;
    auto& c = *db.connection;
    c.execute("create table items (id integer, price numeric(10,2), added date, note varchar(20))");

    c.execute("insert into items values (?, ?, ?, ?)", {"42", "19.99", "2026-01-31", std::nullopt});
    const auto result = c.execute("select id, price, added, note from items where id = ?", {"42"});

    REQUIRE(result.rows.size() == 1);
    CHECK(as<std::int64_t>(result.rows[0][0]) == 42);
    CHECK(as<Decimal>(result.rows[0][1]).text == "19.99");
    CHECK(as<Date>(result.rows[0][2]) == Date{2026, 1, 31});
    CHECK(is_null(result.rows[0][3]));
}

TEST_CASE("parameters are values, never SQL")
{
    TestDatabase db;
    auto& c = *db.connection;
    c.execute("create table t (name varchar(100))");

    c.execute("insert into t values (?)", {"x'); drop table t; --"});

    const auto result = c.execute("select name from t");
    REQUIRE(result.rows.size() == 1);
    CHECK(as<std::string>(result.rows[0][0]) == "x'); drop table t; --");
}

TEST_CASE("the wrong number of parameters is rejected")
{
    TestDatabase db;

    try
    {
        (void)db.connection->execute("select 1 from rdb$database where 1 = ?", {});
        FAIL("expected brezee::core::Error");
    }
    catch (const Error& e)
    {
        CHECK(e.kind() == ErrorKind::InvalidArgument);
    }
}

TEST_CASE("max_rows stops reading and marks the result truncated")
{
    TestDatabase db;
    QueryOptions options;
    options.max_rows = 3;

    const auto result = db.connection->execute(
        "select r from (select 1 as r from rdb$types rows 10)", {}, options);

    CHECK(result.rows.size() == 3);
    CHECK(result.truncated);
}

TEST_CASE("SQL errors become Database errors with Firebird's message, and nothing is left half done")
{
    TestDatabase db;
    auto& c = *db.connection;
    c.execute("create table t (id integer not null primary key)");
    c.execute("insert into t values (1)");

    try
    {
        (void)c.execute("insert into t values (1)");
        FAIL("expected brezee::core::Error");
    }
    catch (const Error& e)
    {
        CHECK(e.kind() == ErrorKind::Database);
        CHECK(std::string(e.what()).find("PRIMARY") != std::string::npos);
    }

    try
    {
        (void)c.execute("select * from no_such_table");
        FAIL("expected brezee::core::Error");
    }
    catch (const Error& e)
    {
        CHECK(std::string(e.what()).find("NO_SUCH_TABLE") != std::string::npos);
    }

    // The connection is still usable after errors.
    CHECK(c.execute("select count(*) from t").rows.size() == 1);
}

TEST_CASE("EXECUTE PROCEDURE returns its output row")
{
    TestDatabase db;
    auto& c = *db.connection;
    c.execute("create procedure add_one (x integer) returns (y integer) as begin y = x + 1; end");

    const auto result = c.execute("execute procedure add_one(?)", {"41"});

    REQUIRE(result.rows.size() == 1);
    CHECK(as<std::int64_t>(result.rows[0][0]) == 42);
}

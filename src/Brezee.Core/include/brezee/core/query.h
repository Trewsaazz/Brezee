#pragma once

#include <cstdint>
#include <optional>
#include <string>
#include <variant>
#include <vector>

namespace brezee::core {

// An exact number that does not fit a 64-bit integer as is: NUMERIC/DECIMAL with a scale, INT128
// and DECFLOAT. Kept as Firebird formats it ("-1234.5600"), so no digit is ever lost.
struct Decimal
{
    std::string text;

    bool operator==(const Decimal&) const = default;
};

struct Date
{
    int year = 0;
    int month = 0;
    int day = 0;

    bool operator==(const Date&) const = default;
};

struct Time
{
    int hours = 0;
    int minutes = 0;
    int seconds = 0;
    int fractions = 0; // Ten-thousandths of a second, as Firebird stores them.

    bool operator==(const Time&) const = default;
};

struct Timestamp
{
    Date date;
    Time time;

    bool operator==(const Timestamp&) const = default;
};

using Bytes = std::vector<std::uint8_t>;

// One field of a result row. Text is UTF-8.
using Value = std::variant<std::monostate, bool, std::int64_t, double, Decimal, std::string, Date, Time, Timestamp, Bytes>;

[[nodiscard]] inline bool is_null(const Value& value) noexcept
{
    return std::holds_alternative<std::monostate>(value);
}

// How a column's values are represented, independent of the exact Firebird type.
enum class ColumnKind
{
    Boolean,   // bool
    Integer,   // std::int64_t (SMALLINT, INTEGER, BIGINT without scale)
    Decimal,   // Decimal (scaled NUMERIC/DECIMAL, INT128, DECFLOAT)
    Float,     // double
    Text,      // std::string (CHAR, VARCHAR, text BLOB, and time zone types as text)
    Date,      // Date
    Time,      // Time
    Timestamp, // Timestamp
    Binary,    // Bytes (binary BLOB, OCTETS strings)
    Unknown,   // Always null (e.g. arrays, which Brezee does not read yet)
};

struct Column
{
    std::string name;     // Alias as it appears in the result, e.g. "CUSTOMER_NAME"
    std::string field;    // Underlying field name, if any
    std::string relation; // Table or view it comes from, if any
    std::string type;     // SQL type as shown to users, e.g. "VARCHAR(40)", "NUMERIC(18,2)"
    ColumnKind kind = ColumnKind::Unknown;
    bool nullable = true;
};

struct QueryResult
{
    std::vector<Column> columns;
    std::vector<std::vector<Value>> rows;

    // True when more rows were available than the requested maximum.
    bool truncated = false;
};

struct QueryOptions
{
    // Stop reading after this many rows (and set QueryResult::truncated). Empty reads everything.
    std::optional<std::size_t> max_rows;

    // BLOBs longer than this are cut off. Protects against loading huge BLOBs by accident.
    std::size_t max_blob_bytes = 16 * 1024 * 1024;
};

// Parameters for "?" placeholders, as text; Firebird converts each to its parameter's real type.
// std::nullopt passes NULL.
using Parameters = std::vector<std::optional<std::string>>;

} // namespace brezee::core

#pragma once

namespace Brezee::Bridge {

// Mirrors brezee::core::ColumnKind: how a column's values arrive in .NET.
public enum class ResultColumnKind
{
    Boolean,   // bool
    Integer,   // long
    Decimal,   // decimal, or string when the value has more digits than System.Decimal holds
    Float,     // double
    Text,      // string
    Date,      // DateOnly
    Time,      // TimeOnly
    Timestamp, // DateTime (unspecified kind)
    Binary,    // byte[]
    Unknown,   // always null
};

// One column of a query result. Mirrors brezee::core::Column.
public ref class ResultColumn sealed
{
public:
    property System::String^ Name;
    property System::String^ Field;
    property System::String^ Relation;
    property System::String^ TypeName;
    property ResultColumnKind Kind;
    property bool IsNullable;
};

// The rows of a query. Each row has one value per column; SQL NULL is null.
public ref class QueryResultData sealed
{
public:
    property System::Collections::Generic::IReadOnlyList<ResultColumn^>^ Columns;
    property System::Collections::Generic::IReadOnlyList<array<System::Object^>^>^ Rows;

    // True when more rows were available than requested.
    property bool Truncated;
};

} // namespace Brezee::Bridge

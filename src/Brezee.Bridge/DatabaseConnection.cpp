#include "DatabaseConnection.h"
#include "CoreErrors.h"
#include "Interop.h"

#include <brezee/core/connection.h>
#include <brezee/core/metadata.h>
#include <brezee/core/query.h>

#include <type_traits>

namespace {

using namespace Brezee::Bridge;

brezee::core::ConnectionParameters ToNative(ConnectionSettings^ settings)
{
    if (settings == nullptr)
        throw gcnew System::ArgumentNullException("settings");
    if (settings->Port < 1 || settings->Port > 65535)
        throw gcnew System::ArgumentOutOfRangeException("settings", settings->Port, "Port must be between 1 and 65535.");

    brezee::core::ConnectionParameters parameters;
    parameters.host = Interop::ToNative(settings->Host);
    parameters.port = static_cast<std::uint16_t>(settings->Port);
    parameters.database = Interop::ToNative(settings->Database);
    parameters.user = Interop::ToNative(settings->User);
    parameters.password = Interop::ToNative(settings->Password);
    parameters.role = Interop::ToNative(settings->Role);
    parameters.charset = Interop::ToNative(settings->Charset);
    return parameters;
}

// Converts one core value to the .NET type documented on ResultColumnKind.
System::Object^ ToManagedValue(const brezee::core::Value& value)
{
    using namespace brezee::core;

    return std::visit([](const auto& v) -> System::Object^ {
        using T = std::decay_t<decltype(v)>;
        if constexpr (std::is_same_v<T, std::monostate>)
            return nullptr;
        else if constexpr (std::is_same_v<T, bool> || std::is_same_v<T, std::int64_t> || std::is_same_v<T, double>)
            return v;
        else if constexpr (std::is_same_v<T, Decimal>)
        {
            auto text = Interop::ToManaged(v.text);
            System::Decimal parsed;
            // Values beyond System.Decimal's 28-29 digits stay text, so nothing is rounded.
            if (System::Decimal::TryParse(text, System::Globalization::NumberStyles::Float,
                    System::Globalization::CultureInfo::InvariantCulture, parsed))
                return parsed;
            return text;
        }
        else if constexpr (std::is_same_v<T, std::string>)
            return Interop::ToManaged(v);
        else if constexpr (std::is_same_v<T, Date>)
            return System::DateOnly(v.year, v.month, v.day);
        else if constexpr (std::is_same_v<T, Time>)
            return System::TimeOnly(System::TimeSpan(v.hours, v.minutes, v.seconds).Ticks + v.fractions * 1000LL);
        else if constexpr (std::is_same_v<T, Timestamp>)
            return System::DateTime(v.date.year, v.date.month, v.date.day, v.time.hours, v.time.minutes, v.time.seconds)
                .AddTicks(v.time.fractions * 1000LL);
        else if constexpr (std::is_same_v<T, Bytes>)
        {
            auto bytes = gcnew array<System::Byte>(static_cast<int>(v.size()));
            for (int i = 0; i < bytes->Length; ++i)
                bytes[i] = v[static_cast<std::size_t>(i)];
            return bytes;
        }
        else
            return nullptr;
    }, value);
}

QueryResultData^ ToManaged(const brezee::core::QueryResult& result)
{
    auto columns = gcnew System::Collections::Generic::List<ResultColumn^>(static_cast<int>(result.columns.size()));
    for (const auto& column : result.columns)
    {
        auto managed = gcnew ResultColumn();
        managed->Name = Interop::ToManaged(column.name);
        managed->Field = Interop::ToManaged(column.field);
        managed->Relation = Interop::ToManaged(column.relation);
        managed->TypeName = Interop::ToManaged(column.type);
        managed->Kind = static_cast<ResultColumnKind>(static_cast<int>(column.kind));
        managed->IsNullable = column.nullable;
        columns->Add(managed);
    }

    auto rows = gcnew System::Collections::Generic::List<array<System::Object^>^>(static_cast<int>(result.rows.size()));
    for (const auto& row : result.rows)
    {
        auto values = gcnew array<System::Object^>(static_cast<int>(row.size()));
        for (int i = 0; i < values->Length; ++i)
            values[i] = ToManagedValue(row[static_cast<std::size_t>(i)]);
        rows->Add(values);
    }

    auto data = gcnew QueryResultData();
    data->Columns = columns->AsReadOnly();
    data->Rows = rows->AsReadOnly();
    data->Truncated = result.truncated;
    return data;
}

} // namespace

namespace Brezee::Bridge {

DatabaseConnection::DatabaseConnection(brezee::core::Connection* connection)
    : _connection(connection)
{
}

DatabaseConnection^ DatabaseConnection::Open(ConnectionSettings^ settings)
{
    const auto parameters = ToNative(settings);
    try
    {
        return gcnew DatabaseConnection(brezee::core::Connection::open(parameters).release());
    }
    catch (const std::exception& e)
    {
        throw ToManagedException(e);
    }
}

DatabaseConnection^ DatabaseConnection::Create(ConnectionSettings^ settings)
{
    const auto parameters = ToNative(settings);
    try
    {
        return gcnew DatabaseConnection(brezee::core::Connection::create(parameters).release());
    }
    catch (const std::exception& e)
    {
        throw ToManagedException(e);
    }
}

DatabaseConnection::~DatabaseConnection()
{
    this->!DatabaseConnection();
}

DatabaseConnection::!DatabaseConnection()
{
    // The native destructor detaches and swallows errors.
    delete _connection;
    _connection = nullptr;
}

bool DatabaseConnection::IsOpen::get()
{
    return _connection != nullptr && _connection->is_open();
}

DatabaseDetails^ DatabaseConnection::GetDetails()
{
    if (_connection == nullptr)
        throw gcnew System::ObjectDisposedException("DatabaseConnection");

    try
    {
        const auto info = _connection->info();

        auto details = gcnew DatabaseDetails();
        details->ServerVersion = Interop::ToManaged(info.server_version);
        details->OdsMajor = info.ods_major;
        details->OdsMinor = info.ods_minor;
        details->PageSize = info.page_size;
        details->SqlDialect = info.sql_dialect;
        return details;
    }
    catch (const std::exception& e)
    {
        throw ToManagedException(e);
    }
}

System::Collections::Generic::IReadOnlyList<DatabaseObjectInfo^>^ DatabaseConnection::ListObjects(
    DatabaseObjectType type, bool includeSystem)
{
    if (_connection == nullptr)
        throw gcnew System::ObjectDisposedException("DatabaseConnection");

    try
    {
        const auto objects = brezee::core::list_objects(
            *_connection, static_cast<brezee::core::ObjectType>(static_cast<int>(type)), includeSystem);

        auto result = gcnew System::Collections::Generic::List<DatabaseObjectInfo^>(static_cast<int>(objects.size()));
        for (const auto& object : objects)
        {
            auto flags = gcnew System::Collections::Generic::List<System::String^>();
            for (const auto& flag : object.flags)
                flags->Add(Interop::ToManaged(flag));

            auto info = gcnew DatabaseObjectInfo();
            info->Type = type;
            info->Name = Interop::ToManaged(object.name);
            info->Parent = Interop::ToManaged(object.parent);
            info->Description = Interop::ToManaged(object.description);
            info->IsSystem = object.is_system;
            info->Flags = flags->AsReadOnly();
            result->Add(info);
        }
        return result->AsReadOnly();
    }
    catch (const std::exception& e)
    {
        throw ToManagedException(e);
    }
}

QueryResultData^ DatabaseConnection::Execute(
    System::String^ sql, System::Collections::Generic::IList<System::String^>^ parameters, int maxRows)
{
    if (_connection == nullptr)
        throw gcnew System::ObjectDisposedException("DatabaseConnection");
    if (sql == nullptr)
        throw gcnew System::ArgumentNullException("sql");
    if (maxRows < 0)
        throw gcnew System::ArgumentOutOfRangeException("maxRows");

    brezee::core::Parameters native;
    if (parameters != nullptr)
    {
        for each (System::String^ parameter in parameters)
        {
            if (parameter == nullptr)
                native.push_back(std::nullopt);
            else
                native.push_back(Interop::ToNative(parameter));
        }
    }

    brezee::core::QueryOptions options;
    if (maxRows > 0)
        options.max_rows = static_cast<std::size_t>(maxRows);

    const auto text = Interop::ToNative(sql);
    try
    {
        return ToManaged(_connection->execute(text, native, options));
    }
    catch (const std::exception& e)
    {
        throw ToManagedException(e);
    }
}

void DatabaseConnection::Close()
{
    if (_connection == nullptr)
        return;

    try
    {
        _connection->close();
    }
    catch (const std::exception& e)
    {
        throw ToManagedException(e);
    }
}

} // namespace Brezee::Bridge

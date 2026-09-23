#include "DatabaseConnection.h"
#include "CoreErrors.h"
#include "Interop.h"

#include <brezee/core/connection.h>
#include <brezee/core/metadata.h>

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
